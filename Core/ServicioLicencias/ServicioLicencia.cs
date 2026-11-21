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
					Dias: Convert.ToInt32(x.DuracionEn(Domain.Shared.Tiempo.Dia)),
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

	/// <summary>
	/// Permiso que exige decidir (aprobar o rechazar) esta licencia: las de resolución académica
	/// son sólo de Secretaría Académica; el resto, de quien tenga <see cref="Permiso.ResolverLicencias"/>.
	/// </summary>
	private static Permiso PermisoParaResolver(Licencia licencia) =>
		licencia.PlazoLegal.ResolucionAcademica ? Permiso.ResolverLicenciasAcademicas : Permiso.ResolverLicencias;

	private static readonly Permiso[] PERMISOS_DE_RESOLUCION = { Permiso.ResolverLicencias, Permiso.ResolverLicenciasAcademicas };

	/// <remarks>
	/// La guarda va en dos pasos porque el actor depende del artículo de la licencia, que recién se
	/// conoce al cargarla. El primero, antes de tocar el request, rechaza a quien no puede resolver
	/// ninguna (así un rol sin permiso no distingue una licencia inexistente de una ajena); el
	/// segundo exige el permiso exacto del artículo.
	/// </remarks>
	public async Task AprobarLicencia(ActualizarEstadoLicenciaRequest request)
	{
		try
		{
			_sesion.ExigirAlguno(PERMISOS_DE_RESOLUCION);

			var licencia = await _unitOfWork.Licencias.BuscarPorIDYDocenteAsync(request.LicenciaID, request.DocenteID);
			if (licencia is null)
			{
				throw new LicenciaNoEncontradaException();
			}

			_sesion.Exigir(PermisoParaResolver(licencia));

			licencia.AprobarLicencia(request.Observacion);

			_unitOfWork.Licencias.Modificar(licencia);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } licencia aprobada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <remarks>Misma guarda en dos pasos que <see cref="AprobarLicencia"/>: quien decide una, decide la otra.</remarks>
	public async Task RechazarLicencia(ActualizarEstadoLicenciaRequest request)
	{
		try
		{
			_sesion.ExigirAlguno(PERMISOS_DE_RESOLUCION);

			var licencia = await _unitOfWork.Licencias.BuscarPorIDYDocenteAsync(request.LicenciaID, request.DocenteID);
			if (licencia is null)
			{
				throw new LicenciaNoEncontradaException();
			}

			_sesion.Exigir(PermisoParaResolver(licencia));

			licencia.CancelarLicencia(request.Observacion);

			_unitOfWork.Licencias.Modificar(licencia);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } licencia cancelada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

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

			_unitOfWork.Licencias.Modificar(licencia);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } licencia modificada correctamente.");
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
}