using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioCatedras.DTOs.Responses;
using Core.ServicioCatedras.Exceptions;
using Core.Shared;
using Domain.Catedras;
using Domain.Catedras.Horarios;
using Domain.Catedras.SituacionesRevista;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioCatedras;

internal class ServicioCatedra : IServicio, IServicioCatedra
{
	private readonly ILogger<ServicioCatedra> _logger;
	private readonly IUnitOfWork _unitOfWork;


	public ServicioCatedra(ILogger<ServicioCatedra> logger, IUnitOfWork unitOfWork)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	#region Búsquedas y mapeos privados
	private async Task<Catedra> BuscarCatedraAsync(Guid unaCatedra)
	{
		var catedra = await _unitOfWork.Catedras.BuscarPorIDAsync(unaCatedra);
		if (catedra is null)
		{
			throw new ArgumentNullException(nameof(unaCatedra), "No se encontró la cátedra.");
		}

		return catedra;
	}

	private static CatedraResponse MapearCatedra(Catedra catedra) =>
		new(
			CatedraID: catedra.Id,
			MateriaID: catedra.MateriaID,
			DivisionID: catedra.DivisionID,
			CargaHoraria: catedra.CargaHoraria,
			HorasAsignadas: catedra.HorasAsignadas,
			HorasSinAsignar: catedra.HorasSinAsignar,
			DocenteEnFuncionesID: catedra.DocenteEnFunciones());

	private static IReadOnlyCollection<CatedraResponse> MapearCatedras(IEnumerable<Catedra> catedras) =>
		catedras.Select(MapearCatedra).ToList();
	#endregion

	#region Crear cátedra
	public async Task<Guid> CrearCatedraAsync(CrearCatedraRequest request)
	{
		try
		{
			var materia = await _unitOfWork.Materias.BuscarPorIDAsync(request.MateriaID);
			if (materia is null)
			{
				throw new ArgumentNullException(nameof(request.MateriaID), "No se encontró la materia.");
			}

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new ArgumentNullException(nameof(request.DivisionID), "No se encontró la división.");
			}

			// Punto de congelamiento: la carga horaria de la cátedra es una instantánea de la
			// materia, legítima porque la currícula que la contiene está vigente.
			var curricula = await _unitOfWork.Curriculas.BuscarPorIDAsync(materia.CurriculaID);
			if (curricula is null || !curricula.EstaVigente())
			{
				throw new ArgumentException("La currícula de la materia no se encuentra vigente.", nameof(request.MateriaID));
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
			var catedra = await BuscarCatedraAsync(request.CatedraID);

			var existeDocente = await _unitOfWork.Docentes.ExisteIDAsync(request.DocenteID);
			if (!existeDocente)
			{
				throw new ArgumentNullException(nameof(request.DocenteID), "No se encontró el docente.");
			}

			// NOTA (violación DDD, ver informe): SituacionRevista.Crear(string) es la factoría que
			// parsea el cargo y lanza CargoInexistenteException, pero es internal. Sin una
			// sobrecarga pública de Catedra.Designar que la use, el parseo queda acá.
			if (!Enum.TryParse<Cargo>(request.Cargo, out var cargo))
			{
				throw new ArgumentException($"El cargo '{ request.Cargo }' no es un cargo docente válido.", nameof(request.Cargo));
			}

			var situacionRevistaID = catedra.Designar(request.DocenteID, cargo, request.FechaInicio, request.FechaFin, request.ReemplazaA);

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
	#endregion

	#region Poner en funciones / Relevar de funciones
	public async Task PonerEnFuncionesAsync(PonerEnFuncionesRequest request)
	{
		try
		{
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

	#region Establecer fin de designación / Finalizar designación
	public async Task EstablecerFinDeDesignacionAsync(EstablecerFinDeDesignacionRequest request)
	{
		try
		{
			var catedra = await BuscarCatedraAsync(request.CatedraID);
			catedra.EstablecerFinDeDesignacion(request.SituacionRevistaID, request.FechaFin);

			_unitOfWork.Catedras.Modificar(catedra);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } fin de designación establecido correctamente.");
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
			var catedra = await BuscarCatedraAsync(request.CatedraID);
			var horario = CrearHorario(request.Turno, request.DiaSemana, request.HoraInicio, request.DuracionHoraCatedra);

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
			var catedra = await BuscarCatedraAsync(request.CatedraID);
			var horario = CrearHorario(request.Turno, request.DiaSemana, request.HoraInicio, request.DuracionHoraCatedra);

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

	private static Horario CrearHorario(string turno, string diaSemana, TimeOnly horaInicio, int duracionHoraCatedra)
	{
		if (!Enum.TryParse<Turno>(turno, out var unTurno))
		{
			throw new ArgumentException($"El turno '{ turno }' no es válido.", nameof(turno));
		}

		if (!Enum.TryParse<Dia>(diaSemana, out var unDia))
		{
			throw new ArgumentException($"El día '{ diaSemana }' no es válido.", nameof(diaSemana));
		}

		return Horario.Crear(unTurno, unDia, horaInicio, duracionHoraCatedra);
	}
	#endregion

	#region Listar cátedras
	public async Task<IReadOnlyCollection<CatedraResponse>> ListarCatedrasSegunMateriaAsync(ListarCatedrasSegunMateriaRequest request)
	{
		try
		{
			var catedras = await _unitOfWork.Catedras.CatedrasSegunMateriaAsync(request.MateriaID);
			return MapearCatedras(catedras);
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
			return MapearCatedras(catedras);
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
			return MapearCatedras(catedras);
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

			return catedra.SituacionesRevista.Select(situacion =>
				new SituacionRevistaResponse(
					SituacionRevistaID: situacion.Id,
					CatedraID: catedra.Id,
					DocenteID: situacion.DocenteID,
					Estado: situacion.Estado.ToString(),
					Cargo: situacion.Cargo.ToString(),
					FechaInicio: situacion.Periodo.FechaInicio,
					FechaFin: situacion.Periodo.FechaFin,
					ReemplazaA: situacion.ReemplazaA,
					EnFunciones: catedra.SituacionEnFuncionesID.HasValue && catedra.SituacionEnFuncionesID.Value.Equals(situacion.Id)))
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
