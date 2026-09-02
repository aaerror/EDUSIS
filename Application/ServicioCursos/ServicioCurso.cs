using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioCursos.Exceptions;
using Core.Shared;
using Domain.Cursos;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioCursos;

internal class ServicioCurso : IServicio, IServicioCurso
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<ServicioCurso> _logger;


	public ServicioCurso(IUnitOfWork unitOfWork, ILogger<ServicioCurso> logger)
	{
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	private async Task<Curso> BuscarCursoAsync(Guid unCurso)
	{
		if (Guid.Empty.Equals(unCurso))
		{
			throw new NullReferenceException($"Datos del curso incompletos o inexistentes. Curso: {unCurso}");
		}

		var cursoBuscado = await _unitOfWork.Cursos.BuscarPorIDAsync(unCurso);
		if (cursoBuscado is null)
		{
			throw new NullReferenceException($"No se encontró el curso buscado: {cursoBuscado}");
		}

		return cursoBuscado;
	}

	#region Registrar, Modificar, Eliminar

	public async Task<IReadOnlyCollection<CursoResponse>> ListarCursosAsync()
	{
		try
		{
			var cursos = await _unitOfWork.Cursos.BuscarTodosAsync();

			return cursos
				.Select(x =>
					new CursoResponse(
						CursoID: x.Id,
						Grado: x.Grado,
						NivelEducativo: x.NivelEducativo,
						Divisiones: x.Divisiones.Count(),
						Alumnos: x.CantidadAlumnos))
				.OrderBy(x => x.Grado)
				.ThenBy(x => x.NivelEducativo)
				.ToList()
				.AsReadOnly();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task RegistrarCurso(RegistrarCursoRequest request)
	{
		try
		{
			var resultG = Enum.TryParse<Grado>(request.Grado, true, out Grado grado);
			var resultNE = Enum.TryParse<NivelEducativo>(request.NivelEducativo, true, out NivelEducativo nivelEducativo);
			if (resultG && resultNE)
			{
				// https://dev.to/karenpayneoregon/using-enum-with-ef-core-20go
				var existeCurso = await _unitOfWork.Cursos
					.BuscarAsync(x => x.Grado.Equals(grado) && x.NivelEducativo.Equals(nivelEducativo));
				if (existeCurso.Any())
				{
					throw new CursoDuplicadoException();
				}

				var nuevoCurso = new Curso(request.Grado, request.NivelEducativo);
				await _unitOfWork.Cursos.AgregarAsync(nuevoCurso);

				var result = await _unitOfWork.GuardarCambiosAsync();

				_logger.LogInformation($"{result} curso registrado correctamente.", result);
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task EliminarCurso(EliminarCursoRequest request)
	{
		try
		{
			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);

			await _unitOfWork.Cursos.Eliminar(request.CursoID);
			// TODO: Eliminar todas las materias y divisiones con este curso asociado
			var result = _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } curso eliminado correctamente", result);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Calificacion
	public async Task RegistrarCalificacion(CrearCalificationRequest request)
	{
		try
		{
			var curso = await BuscarCursoAsync(request.Curso);


		}
		catch (Exception ex)
		{
			throw;
		}
	}
	#endregion

	#region Divisiones
	public async Task<IReadOnlyCollection<DivisionResponse>> BuscarDivisionesAsync(Guid unCurso)
	{
		try
		{
			var divisiones = _unitOfWork.Cursos.DivisionesDelCurso(unCurso);

			_logger.LogInformation($"Se encontraron { divisiones.Count() } divisiones en el curso { unCurso }.");

			var divisionesDelCurso = await Task.WhenAll(divisiones
				.Select(async x =>
					new DivisionResponse(
						DivisionID: x.Id,
						Descripcion: x.Descripcion.Trim(),
						DocenteID: (await _unitOfWork.Docentes.BuscarPorIDAsync(x.Preceptor ?? null))?.Id,
						Docente: (await _unitOfWork.Docentes.BuscarPorIDAsync(x.Preceptor ?? null))?.DatosPersonales.NombreCompleto(),
						Alumnos: x.TotalAlumnos)));

			return divisionesDelCurso
				.OrderBy(x => x.Descripcion)
				.ToList()
				.AsReadOnly();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/*public IReadOnlyCollection<CursanteResponse> BuscarListado(BuscarListadoRequest request)
	{
		try
		{
			var curso = BuscarCurso(request.CursoID);
			var alumnos = curso.CursantesPorPeriodo(request.DivisionID, request.Periodo);

			return alumnos.Select(x => new CursanteResponse(x,
															_unitOfWork.Alumnos.BuscarPorID(x).InformacionPersonal.NombreCompleto(),
															_unitOfWork.Alumnos.BuscarPorID(x).InformacionPersonal.Documento,
															_unitOfWork.Alumnos.BuscarPorID(x).InformacionPersonal.Edad().ToString()))
						  .ToList()
						  .AsReadOnly();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public void InscribirAlumnoEnDivision(CrearCursanteRequest request)
	{
		try
		{
			var curso = BuscarCurso(request.CursoID);

			curso.AgregarAlumno(request.DivisionID, request.AlumnoID, request.Periodo);

			_unitOfWork.Cursos.Modificar(curso);
			_unitOfWork.GuardarCambiosAsync();
		}
		catch (Exception ex)
		{
			throw;
		}
	}*/

	public async Task RegistrarPreceptorEnDivision(RegistrarPreceptorRequest request)
	{
		try
		{
			var curso = await BuscarCursoAsync(request.CursoID);
			curso.AsignarPreceptor(request.DivisionID, request.DocenteID);

			_unitOfWork.Cursos.Modificar(curso);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } docente asignado correctamente.", result);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task EliminarPreceptorDeDivision(EliminarPreceptorRequest request)
	{
		try
		{
			var curso = await BuscarCursoAsync(request.CursoID);
			curso.QuitarPreceptor(request.DivisionID);

			_unitOfWork.Cursos.Modificar(curso);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{result} curso modificado correctamente.", result);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }");
			throw;
		}
	}

	public async Task AgregarDivisionAlCurso(Guid unCurso)
	{
		try
		{
			var curso = _unitOfWork.Cursos.CursoConDivisiones(unCurso);
			curso.AgregarDivision();

			_unitOfWork.Cursos.Modificar(curso);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{result} división agregada al curso correctamente.", result);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task QuitarDivisiosDelCurso(EliminarDivisionRequest request)
	{
		try
		{
			var curso = _unitOfWork.Cursos.CursoConDivisiones(request.CursoID);
			curso.QuitarDivision(request.DivisionID);

			_unitOfWork.Cursos.Modificar(curso);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{result} división eliminada del curso correctamente.", result);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}



	public async Task RegistrarCursanteEnDivision(RegistrarCursanteRequest request)
	{
		_logger.LogInformation($"Asignando curso al alumno...");
		try
		{
			var curso = await BuscarCursoAsync(request.CursoID);
			curso.AgregarAlumnoEnDivision(request.DivisionID, request.AlumnoID);

			_unitOfWork.Cursos.Modificar(curso);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } docente asignado correctamente.", result);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
