using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes.DTOs.Responses;
using Core.ServicioCursantes.Exceptions;
using Core.Shared;
using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioCursantes;

internal class ServicioCursante : IServicio, IServicioCursante
{
	private readonly ILogger<ServicioCursante> _logger;
	private readonly IUnitOfWork _unitOfWork;


	public ServicioCursante(ILogger<ServicioCursante> logger, IUnitOfWork unitOfWork)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	#region Inscripción
	public async Task InscribirCursanteAsync(RegistrarCursanteRequest request)
	{
		try
		{
			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new InvalidOperationException("El curso no existe.");
			}

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new NullReferenceException("La división no existe.");
			}

			var cicloLectivo = CicloLectivo.Crear(request.Periodo);

			var existeInscripcion = await _unitOfWork.Cursantes.ExisteInscripcionAsync(request.AlumnoID, cicloLectivo);
			if (existeInscripcion)
			{
				throw new InscripcionDuplicadaException();
			}

			var cantidadCursantes = await _unitOfWork.Cursantes.ContarCursantesDeDivisionAsync(division.Id, cicloLectivo);
			division.ValidarCupoDisponible(cantidadCursantes);

			var cursante = new Cursante(division.Id, request.AlumnoID, cicloLectivo, DateTime.Today, request.EsRecursante);

			await _unitOfWork.Cursantes.AgregarAsync(cursante);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } cursante inscripto correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Listado
	public async Task<IReadOnlyCollection<CursanteResponse>> ListarCursantesAsync(BuscarListadoRequest request)
	{
		try
		{
			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new InvalidOperationException("El curso no existe.");
			}

			var cicloLectivo = CicloLectivo.Crear(request.Periodo);
			var cursantes = await _unitOfWork.Cursantes.BuscarPorDivisionAsync(request.DivisionID, cicloLectivo);

			var respuesta = new List<CursanteResponse>();
			foreach (var cursante in cursantes)
			{
				var alumno = await _unitOfWork.Alumnos.BuscarPorIDAsync(cursante.AlumnoID);
				if (alumno is null)
				{
					continue;
				}

				respuesta.Add(new CursanteResponse(
					CursanteID: cursante.Id,
					AlumnoID: cursante.AlumnoID,
					NombreCompleto: alumno.DatosPersonales.NombreCompleto(),
					Documento: alumno.DatosPersonales.Documento,
					Edad: alumno.DatosPersonales.Edad(),
					EsRecursante: cursante.EsRecursante));
			}

			return respuesta.AsReadOnly();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task<IReadOnlyCollection<CalificacionResponse>> ListarCalificacionesAsync(ListarCalificacionesRequest request)
	{
		try
		{
			var cicloLectivo = CicloLectivo.Crear(request.Periodo);
			var cursante = await _unitOfWork.Cursantes.BuscarInscripcionActivaAsync(request.AlumnoID, cicloLectivo);
			if (cursante is null)
			{
				throw new NullReferenceException("No se encontró la inscripción del alumno en el ciclo lectivo indicado.");
			}

			var respuesta = new List<CalificacionResponse>();
			foreach (var calificacion in cursante.Calificaciones)
			{
				var materia = await _unitOfWork.Materias.BuscarPorIDAsync(calificacion.MateriaID);

				respuesta.Add(new CalificacionResponse(
					CalificacionID: calificacion.Id,
					MateriaID: calificacion.MateriaID,
					Materia: materia?.Descripcion ?? string.Empty,
					Fecha: calificacion.Fecha,
					Instancia: calificacion.Instancia.ToString(),
					Rindio: calificacion.Rindio,
					Nota: calificacion.Nota,
					Aprobado: calificacion.EstaAprobado(),
					Observacion: calificacion.Observacion));
			}

			return respuesta.AsReadOnly();
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Calificaciones
	public async Task<Guid> RegistrarCalificacionAsync(CrearCalificationRequest request)
	{
		try
		{
			var cicloLectivo = CicloLectivo.Crear(request.Periodo);
			var cursante = await _unitOfWork.Cursantes.BuscarInscripcionActivaAsync(request.AlumnoID, cicloLectivo);
			if (cursante is null)
			{
				throw new NullReferenceException("No se encontró la inscripción del alumno en el ciclo lectivo indicado.");
			}

			var resultInstancia = Enum.TryParse<Instancia>(request.Instancia, true, out var instancia);
			if (!resultInstancia)
			{
				throw new ArgumentException("La instancia especificada no existe.", nameof(request.Instancia));
			}

			var calificacionID = cursante.RegistrarCalificacion(request.MateriaID, request.Fecha, instancia, request.Nota, request.Observacion);

			_unitOfWork.Cursantes.Modificar(cursante);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } calificación registrada correctamente.");

			return calificacionID;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task<Guid> RegistrarInasistenciaAExamenAsync(RegistrarInasistenciaRequest request)
	{
		try
		{
			var cicloLectivo = CicloLectivo.Crear(request.Periodo);
			var cursante = await _unitOfWork.Cursantes.BuscarInscripcionActivaAsync(request.AlumnoID, cicloLectivo);
			if (cursante is null)
			{
				throw new NullReferenceException("No se encontró la inscripción del alumno en el ciclo lectivo indicado.");
			}

			var resultInstancia = Enum.TryParse<Instancia>(request.Instancia, true, out var instancia);
			if (!resultInstancia)
			{
				throw new ArgumentException("La instancia especificada no existe.", nameof(request.Instancia));
			}

			var calificacionID = cursante.RegistrarInasistenciaAExamen(request.MateriaID, request.Fecha, instancia, request.Observacion);

			_unitOfWork.Cursantes.Modificar(cursante);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } inasistencia a examen registrada correctamente.");

			return calificacionID;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task QuitarCalificacionAsync(EliminarCalificacionRequest request)
	{
		try
		{
			var cicloLectivo = CicloLectivo.Crear(request.Periodo);
			var cursante = await _unitOfWork.Cursantes.BuscarInscripcionActivaAsync(request.AlumnoID, cicloLectivo);
			if (cursante is null)
			{
				throw new NullReferenceException("No se encontró la inscripción del alumno en el ciclo lectivo indicado.");
			}

			cursante.QuitarCalificacion(request.CalificacionID);

			_unitOfWork.Cursantes.Modificar(cursante);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } calificación eliminada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task ModificarObservacionCalificacionAsync(ModificarObservacionCalificacionRequest request)
	{
		try
		{
			var cicloLectivo = CicloLectivo.Crear(request.Periodo);
			var cursante = await _unitOfWork.Cursantes.BuscarInscripcionActivaAsync(request.AlumnoID, cicloLectivo);
			if (cursante is null)
			{
				throw new NullReferenceException("No se encontró la inscripción del alumno en el ciclo lectivo indicado.");
			}

			cursante.ModificarObservacionCalificacion(request.CalificacionID, request.Observacion);

			_unitOfWork.Cursantes.Modificar(cursante);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } observación de calificación modificada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
