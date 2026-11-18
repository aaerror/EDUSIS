using Domain.Curriculas.Exceptions;
using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioCatedras.DTOs.Responses;
using Core.ServicioCatedras.Exceptions;
using Core.ServicioDivisiones.Exceptions;
using Core.ServicioDocentes.Exceptions;
using Core.ServicioMaterias.Exceptions;
using Core.Shared;
using Core.Shared.Seguridad;
using Domain.Catedras;
using Domain.Catedras.Exceptions;
using Domain.Catedras.Horarios;
using Domain.Catedras.SituacionesRevista;
using Domain.Shared;
using Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Core.ServicioCatedras;

internal class ServicioCatedra : IServicio, IServicioCatedra
{
	private readonly ILogger<ServicioCatedra> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ISesionActual _sesion;


	public ServicioCatedra(ILogger<ServicioCatedra> logger, IUnitOfWork unitOfWork, ISesionActual sesion)
	{
		_sesion = sesion;
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	#region Búsquedas y mapeos privados
	private async Task<Catedra> BuscarCatedraAsync(Guid unaCatedra)
	{
		var catedra = await _unitOfWork.Catedras.BuscarPorIDAsync(unaCatedra);
		if (catedra is null)
		{
			throw new CatedraNoEncontradaException(nameof(unaCatedra));
		}

		return catedra;
	}

	/// <summary>
	/// Proyecta las cátedras con los NOMBRES de lo que referencian (materia, división y docente en
	/// funciones), resueltos en tres consultas para todo el listado.
	/// </summary>
	/// <remarks>
	/// <c>Catedra</c> referencia a los otros agregados sólo por identificador, y antes el response
	/// llevaba nada más que esos identificadores: la pantalla tenía que pedir los listados de
	/// divisiones y docentes y cruzarlos en memoria. El cruce es un modelo de lectura y vive acá.
	/// </remarks>
	private async Task<IReadOnlyCollection<CatedraResponse>> MapearCatedrasAsync(IReadOnlyCollection<Catedra> catedras)
	{
		var materias = await _unitOfWork.Materias.DescripcionesAsync(catedras.Select(x => x.MateriaID));
		var divisiones = await _unitOfWork.Divisiones.DescripcionesAsync(catedras.Select(x => x.DivisionID));
		var docentes = await _unitOfWork.Docentes.NombresAsync(
			catedras.Select(x => x.DocenteEnFunciones()).OfType<Guid>());

		return catedras.Select(catedra =>
		{
			var docenteEnFunciones = catedra.DocenteEnFunciones();

			return new CatedraResponse(
				CatedraID: catedra.Id,
				MateriaID: catedra.MateriaID,
				DivisionID: catedra.DivisionID,
				CargaHoraria: catedra.CargaHoraria,
				HorasAsignadas: catedra.HorasAsignadas,
				HorasSinAsignar: catedra.HorasSinAsignar,
				DocenteEnFuncionesID: docenteEnFunciones,
				Materia: materias.GetValueOrDefault(catedra.MateriaID, string.Empty),
				Division: divisiones.GetValueOrDefault(catedra.DivisionID, string.Empty),
				DocenteEnFunciones: docenteEnFunciones.HasValue
					? docentes.GetValueOrDefault(docenteEnFunciones.Value, DOCENTE_SIN_LEGAJO)
					: null);
		}).ToList();
	}

	/// <summary>Texto para una referencia a un docente cuyo legajo ya no existe.</summary>
	private const string DOCENTE_SIN_LEGAJO = "Docente sin legajo";
	#endregion

	#region Crear cátedra
	public async Task<Guid> CrearCatedraAsync(CrearCatedraRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCatedras);

			var materia = await _unitOfWork.Materias.BuscarPorIDAsync(request.MateriaID);
			if (materia is null)
			{
				throw new MateriaNoEncontradaException(nameof(request.MateriaID));
			}

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new DivisionNoEncontradaException(nameof(request.DivisionID));
			}

			// Punto de congelamiento: la carga horaria de la cátedra es una instantánea de la
			// materia, legítima porque la currícula que la contiene está vigente.
			var curricula = await _unitOfWork.Curriculas.BuscarPorIDAsync(materia.CurriculaID);
			if (curricula is null || !curricula.EstaVigente())
			{
				throw new CurriculaNoVigenteException();
			}

			var existeCatedra = await _unitOfWork.Catedras.ExisteCatedraAsync(request.MateriaID, request.DivisionID);
			if (existeCatedra)
			{
				throw new CatedraDuplicadaException();
			}

			var catedra = new Catedra(request.MateriaID, request.DivisionID, materia.HorasCatedra);

			await _unitOfWork.Catedras.AgregarAsync(catedra);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } cátedra registrada correctamente.");

			return catedra.Id;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Designar docente
	public async Task<Guid> DesignarDocenteAsync(DesignarDocenteRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCatedras);

			var (catedra, situacionRevistaID) = await DesignarAsync(request);

			_unitOfWork.Catedras.Modificar(catedra);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } docente designado correctamente.");

			return situacionRevistaID;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <remarks>
	/// Dos <c>SaveChanges</c> dentro de una transacción, no uno: insertar la situación de revista y
	/// escribir <c>situacion_en_funciones_id</c> en el mismo guardado choca con el ciclo
	/// <c>FK_CATEDRA_SITUACION_REVISTA</c> ↔ <c>FK_SITUACION_REVISTA_CATEDRA</c> (EF aborta con
	/// "circular dependency"). La transacción es lo que vuelve atómico el par: antes eran dos casos
	/// de uso y, si fallaba el segundo, la designación quedaba guardada sin el docente en el aula.
	/// <para>
	/// El rollback va en un try propio que arranca después de <c>BeginTransactionAsync</c>, como en
	/// <c>ServicioCurricula.RegistrarCurriculaAsync</c>: si la guarda de permiso cayera dentro,
	/// el rollback sin transacción iniciada taparía el <c>PermisoDenegadoException</c>.
	/// </para>
	/// </remarks>
	public async Task<Guid> DesignarYPonerEnFuncionesAsync(DesignarDocenteRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCatedras);

			await _unitOfWork.BeginTransactionAsync();

			try
			{
				var (catedra, situacionRevistaID) = await DesignarAsync(request);

				_unitOfWork.Catedras.Modificar(catedra);
				await _unitOfWork.GuardarCambiosAsync();

				catedra.PonerEnFunciones(situacionRevistaID);
				await _unitOfWork.GuardarCambiosAsync();

				await _unitOfWork.CommitTransactionAsync();

				_logger.LogInformation("Docente designado y puesto en funciones correctamente.");

				return situacionRevistaID;
			}
			catch
			{
				await _unitOfWork.RollbackTransactionAsync();
				throw;
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	private async Task<(Catedra Catedra, Guid SituacionRevistaID)> DesignarAsync(DesignarDocenteRequest request)
	{
		var catedra = await BuscarCatedraAsync(request.CatedraID);

		var existeDocente = await _unitOfWork.Docentes.ExisteIDAsync(request.DocenteID);
		if (!existeDocente)
		{
			throw new DocenteNoEncontradoException(nameof(request.DocenteID));
		}

		var situacionRevistaID = catedra.Designar(request.DocenteID, request.Cargo, request.FechaInicio, request.FechaFin, request.ReemplazaA);

		return (catedra, situacionRevistaID);
	}
	#endregion

	#region Poner en funciones / Relevar de funciones
	public async Task PonerEnFuncionesAsync(PonerEnFuncionesRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCatedras);

			var catedra = await BuscarCatedraAsync(request.CatedraID);
			catedra.PonerEnFunciones(request.SituacionRevistaID);

			_unitOfWork.Catedras.Modificar(catedra);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } docente puesto en funciones correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task RelevarDeFuncionesAsync(RelevarDeFuncionesRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCatedras);

			var catedra = await BuscarCatedraAsync(request.CatedraID);
			catedra.RelevarDeFunciones();

			_unitOfWork.Catedras.Modificar(catedra);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } docente relevado de funciones correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Pasar a interino / Finalizar designación
	public async Task PasarAInterinoAsync(PasarAInterinoRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCatedras);

			var catedra = await BuscarCatedraAsync(request.CatedraID);
			catedra.PasarAInterinoHasta(request.SituacionRevistaID, request.FechaFin);

			_unitOfWork.Catedras.Modificar(catedra);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } designación pasada a interina correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task FinalizarDesignacionAsync(FinalizarDesignacionRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCatedras);

			var catedra = await BuscarCatedraAsync(request.CatedraID);
			catedra.FinalizarDesignacion(request.SituacionRevistaID);

			_unitOfWork.Catedras.Modificar(catedra);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } designación finalizada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Agregar / Quitar horario
	public async Task AgregarHorarioAsync(AgregarHorarioRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCatedras);

			var catedra = await BuscarCatedraAsync(request.CatedraID);
			var horario = Horario.Crear(request.Turno, request.DiaSemana, request.HoraInicio, request.DuracionHoraCatedra);

			// La colisión de horarios en la división y el docente en dos aulas a la vez son
			// reglas que cruzan cátedras: ningún agregado puede verlas, así que el barrido lo
			// hace Core sobre los repositorios y Domain sólo aporta Horario.SeSuperponeCon.
			var catedrasDeLaDivision = await _unitOfWork.Catedras.CatedrasSegunDivisionAsync(catedra.DivisionID);
			var hayColisionEnDivision = catedrasDeLaDivision
				.Where(otra => !otra.Id.Equals(catedra.Id))
				.SelectMany(otra => otra.Horarios)
				.Any(otroHorario => otroHorario.SeSuperponeCon(horario));
			if (hayColisionEnDivision)
			{
				throw new ColisionHorariaEnDivisionException();
			}

			var docenteEnFunciones = catedra.DocenteEnFunciones();
			if (docenteEnFunciones.HasValue)
			{
				var catedrasDelDocente = await _unitOfWork.Catedras.CatedrasSegunDocenteAsync(docenteEnFunciones.Value);
				var hayColisionDeDocente = catedrasDelDocente
					.Where(otra => !otra.Id.Equals(catedra.Id))
					.SelectMany(otra => otra.Horarios)
					.Any(otroHorario => otroHorario.SeSuperponeCon(horario));
				if (hayColisionDeDocente)
				{
					throw new DocenteEnDosAulasException();
				}
			}

			catedra.AgregarHorario(horario);

			_unitOfWork.Catedras.Modificar(catedra);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } horario agregado correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task QuitarHorarioAsync(QuitarHorarioRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCatedras);

			var catedra = await BuscarCatedraAsync(request.CatedraID);
			var horario = Horario.Crear(request.Turno, request.DiaSemana, request.HoraInicio, request.DuracionHoraCatedra);

			catedra.QuitarHorario(horario);

			_unitOfWork.Catedras.Modificar(catedra);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } horario quitado correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	#endregion

	#region Listar cátedras
	public async Task<IReadOnlyCollection<CatedraResponse>> ListarCatedrasSegunMateriaAsync(ListarCatedrasSegunMateriaRequest request)
	{
		try
		{
			var catedras = await _unitOfWork.Catedras.CatedrasSegunMateriaAsync(request.MateriaID);
			return await MapearCatedrasAsync(catedras);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task<IReadOnlyCollection<CatedraResponse>> ListarCatedrasSegunDivisionAsync(ListarCatedrasSegunDivisionRequest request)
	{
		try
		{
			var catedras = await _unitOfWork.Catedras.CatedrasSegunDivisionAsync(request.DivisionID);
			return await MapearCatedrasAsync(catedras);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task<IReadOnlyCollection<CatedraResponse>> ListarCatedrasSegunDocenteAsync(ListarCatedrasSegunDocenteRequest request)
	{
		try
		{
			var catedras = await _unitOfWork.Catedras.CatedrasSegunDocenteAsync(request.DocenteID);
			return await MapearCatedrasAsync(catedras);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Listar situaciones de revista / horarios
	public async Task<IReadOnlyCollection<SituacionRevistaResponse>> ListarSituacionesRevistaAsync(ListarSituacionesRevistaRequest request)
	{
		try
		{
			var catedra = await BuscarCatedraAsync(request.CatedraID);

			// Una sola consulta para todos los nombres, incluidos los de docentes dados de baja: el
			// historial existe justamente para mostrarlos.
			var nombres = await _unitOfWork.Docentes.NombresAsync(catedra.SituacionesRevista.Select(x => x.DocenteID));

			// De la más reciente a la más vieja: la grilla es también el historial de la cátedra, y lo
			// que se busca en un historial es lo último que pasó.
			return catedra.SituacionesRevista
				.OrderByDescending(situacion => situacion.Periodo.FechaInicio)
				.Select(situacion =>
				{
					var reemplazaADocenteID = situacion.ReemplazaA.HasValue ? catedra.DocenteDe(situacion.ReemplazaA.Value) : null;

					return new SituacionRevistaResponse(
						SituacionRevistaID: situacion.Id,
						CatedraID: catedra.Id,
						DocenteID: situacion.DocenteID,
						Estado: situacion.Estado.ToString(),
						Cargo: situacion.Cargo.ToString(),
						FechaInicio: situacion.Periodo.FechaInicio,
						FechaFin: situacion.Periodo.FechaFin,
						ReemplazaA: situacion.ReemplazaA,
						EnFunciones: catedra.SituacionEnFuncionesID.HasValue && catedra.SituacionEnFuncionesID.Value.Equals(situacion.Id),
						Vigente: situacion.EsCargoVigente(),
						EsOcupante: situacion.EsOcupante(),
						// El salto de designación reemplazada a docente reemplazado. Se resuelve contra
						// la misma colección ya cargada, así que no cuesta una consulta más.
						ReemplazaADocenteID: reemplazaADocenteID,
						TieneSuplenteVigente: catedra.TieneSuplenteVigente(situacion.Id),
						AdmitePasarAInterino: situacion.AdmitePasarAInterino(),
						Docente: nombres.GetValueOrDefault(situacion.DocenteID, DOCENTE_SIN_LEGAJO),
						ReemplazaADocente: reemplazaADocenteID.HasValue
							? nombres.GetValueOrDefault(reemplazaADocenteID.Value, DOCENTE_SIN_LEGAJO)
							: null);
				})
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task<IReadOnlyCollection<HorarioResponse>> ListarHorariosSegunCatedraAsync(ListarHorariosRequest request)
	{
		try
		{
			var catedra = await BuscarCatedraAsync(request.CatedraID);

			return catedra.Horarios.Select(horario =>
				new HorarioResponse(
					Turno: horario.Turno.ToString(),
					Dia: horario.DiaSemana.ToString(),
					HoraInicio: horario.HoraInicio,
					HoraFin: horario.HoraFin))
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
