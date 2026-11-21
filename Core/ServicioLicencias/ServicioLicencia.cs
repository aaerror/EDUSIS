using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.Exceptions;
using Core.ServicioLicencias.Exceptions;
using Core.ServicioLicencias.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Responses;
using Core.Shared;
using Domain.Licencias;
using Domain.Shared;
using Microsoft.Extensions.Logging;
using Core.Shared.Seguridad;
using Domain.Usuarios;

namespace Core.ServicioLicencias;

internal class ServicioLicencia : IServicio, IServicioLicencia
{
	private readonly ILogger<ServicioLicencia> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ISesionActual _sesion;


	public ServicioLicencia(ILogger<ServicioLicencia> logger, IUnitOfWork unitOfWork, ISesionActual sesion)
	{
		_sesion = sesion;
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	#region Consultas
	public async Task<IReadOnlyCollection<LicenciaResponse>> BuscarLicenciasSegunDocenteAsync(DocenteIDRequest request)
	{
		try
		{
			var licencias = await _unitOfWork.Licencias.BuscarLicenciasDeDocenteAsync(request.DocenteID);
			return licencias.Select(x =>
				new LicenciaResponse(
					LicenciaID: x.Id,
					DocenteID: x.DocenteID,
					Articulo: x.Articulo,
					Estado: x.Estado.ToString(),
					Dias: x.Dias,
					FechaInicio: x.Periodo.FechaInicio.Date,
					FechaFin: x.Periodo.FechaFin,
					Observacion: x.Observacion,
					Vencida: x.EstaVencida()))
			.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <remarks>
	/// Sin guarda, como toda consulta: filtra por el permiso del artículo en lugar de rechazar, así
	/// una sesión sin ningún permiso de resolución recibe una bandeja vacía. Decidir sigue exigiendo
	/// el permiso en <see cref="AprobarLicencia"/> y <see cref="RechazarLicencia"/>.
	/// </remarks>
	public async Task<IReadOnlyCollection<LicenciaPendienteResponse>> ListarLicenciasPendientesAsync()
	{
		try
		{
			var pendientes = (await _unitOfWork.Licencias.BuscarPendientesAsync())
				.Where(licencia => _sesion.TienePermiso(PermisoParaResolver(licencia)))
				.ToList();

			var nombres = await _unitOfWork.Docentes.NombresAsync(pendientes.Select(x => x.DocenteID).Distinct());

			return pendientes.Select(x =>
				new LicenciaPendienteResponse(
					LicenciaID: x.Id,
					DocenteID: x.DocenteID,
					Docente: nombres.TryGetValue(x.DocenteID, out var nombre) ? nombre : string.Empty,
					Articulo: x.Articulo,
					Dias: x.Dias,
					FechaInicio: x.Periodo.FechaInicio.Date,
					FechaFin: x.Periodo.FechaFin,
					Observacion: x.Observacion))
			.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Resolución
	/// <summary>
	/// Permiso que exige decidir esta licencia y operar sobre ella una vez aprobada: las de
	/// resolución académica son sólo de Secretaría Académica; el resto, de quien tenga
	/// <see cref="Permiso.ResolverLicencias"/>.
	/// </summary>
	private static Permiso PermisoParaResolver(Licencia licencia) =>
		licencia.PlazoLegal.ResolucionAcademica ? Permiso.ResolverLicenciasAcademicas : Permiso.ResolverLicencias;

	private static readonly Permiso[] PERMISOS_DE_RESOLUCION = { Permiso.ResolverLicencias, Permiso.ResolverLicenciasAcademicas };

	/// <summary>
	/// Carga la licencia para un caso de uso de quien la resuelve (aprobar, rechazar, y finalizar,
	/// prorrogar o cambiar la duración de una aprobada) y exige el permiso exacto de su artículo.
	/// </summary>
	/// <remarks>
	/// La guarda va en dos pasos porque el actor depende del artículo de la licencia, que recién se
	/// conoce al cargarla. El primero, <c>ExigirAlguno(PERMISOS_DE_RESOLUCION)</c>, lo escribe cada
	/// caso de uso como su primera línea, ANTES de tocar el request: rechaza a quien no puede
	/// resolver ninguna (así un rol sin permiso no distingue una licencia inexistente de una ajena,
	/// y un request nulo no le llega como <c>NullReferenceException</c>). El segundo va acá.
	/// </remarks>
	private async Task<Licencia> CargarParaResolverAsync(Guid licenciaID, Guid docenteID)
	{
		var licencia = await _unitOfWork.Licencias.BuscarPorIDYDocenteAsync(licenciaID, docenteID);
		if (licencia is null)
		{
			throw new LicenciaNoEncontradaException();
		}

		_sesion.Exigir(PermisoParaResolver(licencia));

		return licencia;
	}

	/// <summary>
	/// Un docente tiene a lo sumo una licencia por período: se invoca con el período YA aplicado
	/// por el agregado y antes de guardar, así la licencia nunca llega a la base superpuesta.
	/// </summary>
	/// <remarks>
	/// Se excluye a sí misma: la base todavía tiene el período anterior de la que se modifica.
	/// </remarks>
	private async Task ValidarSinSuperposicionAsync(Licencia licencia)
	{
		if (await _unitOfWork.Licencias.ExisteSuperpuestaAsync(licencia.DocenteID, licencia.Periodo, licencia.Id))
		{
			throw new LicenciaSuperpuestaException();
		}
	}

	private async Task GuardarAsync(Licencia licencia, string operacion)
	{
		_unitOfWork.Licencias.Modificar(licencia);
		var result = await _unitOfWork.GuardarCambiosAsync();

		_logger.LogInformation($"{ result } licencia { operacion } correctamente.");
	}

	public async Task AprobarLicencia(ActualizarEstadoLicenciaRequest request)
	{
		try
		{
			_sesion.ExigirAlguno(PERMISOS_DE_RESOLUCION);

			var licencia = await CargarParaResolverAsync(request.LicenciaID, request.DocenteID);

			licencia.AprobarLicencia(request.Observacion);

			await GuardarAsync(licencia, "aprobada");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <remarks>Misma guarda que <see cref="AprobarLicencia"/>: quien decide una, decide la otra.</remarks>
	public async Task RechazarLicencia(ActualizarEstadoLicenciaRequest request)
	{
		try
		{
			_sesion.ExigirAlguno(PERMISOS_DE_RESOLUCION);

			var licencia = await CargarParaResolverAsync(request.LicenciaID, request.DocenteID);

			licencia.RechazarLicencia(request.Observacion);

			await GuardarAsync(licencia, "rechazada");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Licencia aprobada
	/// <remarks>
	/// Registra el reintegro. El regreso del docente a sus cátedras lo hace
	/// <c>LicenciaFinalizadaEventHandler</c> dentro de este mismo guardado. Acortar el período no
	/// puede crear una superposición, así que no se la controla.
	/// </remarks>
	public async Task FinalizarLicencia(FinalizarLicenciaRequest request)
	{
		try
		{
			_sesion.ExigirAlguno(PERMISOS_DE_RESOLUCION);

			var licencia = await CargarParaResolverAsync(request.LicenciaID, request.DocenteID);

			licencia.FinalizarLicencia();

			await GuardarAsync(licencia, "finalizada");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task ExtenderLicencia(ExtenderLicenciaRequest request)
	{
		try
		{
			_sesion.ExigirAlguno(PERMISOS_DE_RESOLUCION);

			var licencia = await CargarParaResolverAsync(request.LicenciaID, request.DocenteID);

			licencia.ExtenderLicencia(request.Dias);
			await ValidarSinSuperposicionAsync(licencia);

			await GuardarAsync(licencia, "extendida");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <remarks>
	/// Sólo sobre una licencia indefinida: le pone término, así que acorta el período y no se
	/// controla la superposición.
	/// </remarks>
	public async Task FijarFechaFinLicencia(FijarFechaFinLicenciaRequest request)
	{
		try
		{
			_sesion.ExigirAlguno(PERMISOS_DE_RESOLUCION);

			var licencia = await CargarParaResolverAsync(request.LicenciaID, request.DocenteID);

			licencia.EstablecerFechaFinalizacion(request.FechaFin);

			await GuardarAsync(licencia, "con fecha de fin");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task DeclararLicenciaIndefinida(DeclararLicenciaIndefinidaRequest request)
	{
		try
		{
			_sesion.ExigirAlguno(PERMISOS_DE_RESOLUCION);

			var licencia = await CargarParaResolverAsync(request.LicenciaID, request.DocenteID);

			licencia.EstablecerLicenciaIndefinida();
			await ValidarSinSuperposicionAsync(licencia);

			await GuardarAsync(licencia, "declarada indefinida");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Solicitud
	public async Task ModificarLicencia(ModificarLicenciaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarDocentes);

			var licencia = await _unitOfWork.Licencias.BuscarPorIDYDocenteAsync(request.LicenciaID, request.DocenteID);
			if (licencia is null)
			{
				throw new LicenciaNoEncontradaException();
			}

			licencia.ModificarSolicitud(request.Articulo, request.FechaInicio, request.Dias);
			licencia.ModificarObservaciones(request.Observacion);
			await ValidarSinSuperposicionAsync(licencia);

			await GuardarAsync(licencia, "modificada");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task EliminarLicencia(EliminarLicenciaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarDocentes);

			var licencia = await _unitOfWork.Licencias.BuscarPorIDYDocenteAsync(request.LicenciaID, request.DocenteID);
			if (licencia is null)
			{
				throw new LicenciaNoEncontradaException();
			}

			// La regla es del agregado y la aplica el agregado: acá sólo se la invoca. Comparar
			// licencia.Estado en el servicio sería suplir una validación del dominio.
			licencia.ValidarSePuedeEliminar();

			await _unitOfWork.Licencias.EliminarAsync(licencia.Id);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } licencia eliminada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task SolicitarLicencia(NuevaSolicitudLicenciaRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarDocentes);

			// DocenteNoEncontradoException y no "Datos incompletos para registrar la licencia": esta
			// guarda pregunta si el docente existe, y el mensaje anterior describía otra cosa. Llegaba
			// tal cual al usuario —las excepciones suben hasta el ViewModel sin ser tragadas—, así que
			// quien solicitaba una licencia para un docente inexistente leía que sus datos estaban
			// incompletos. Agrupar los throw del BCL por el texto de su mensaje puso este sitio en la
			// familia de las guardas de request nulo, que es lo que el mensaje decía y no lo que el
			// código hace.
			var existeDocente = await _unitOfWork.Docentes.ExisteIDAsync(request.DocenteID);
			if (!existeDocente)
			{
				throw new DocenteNoEncontradoException(nameof(request.DocenteID));
			}

			// El período (inicio + días corridos, o sin fin si son cero) y su plazo legal los decide
			// el agregado: el servicio ya no calcula la fecha de fin ni elige el constructor.
			var licencia = new Licencia(request.DocenteID, request.Articulo, request.FechaInicio, request.Dias, request.Observacion);
			await ValidarSinSuperposicionAsync(licencia);

			await _unitOfWork.Licencias.AgregarAsync(licencia);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } licencia creada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
