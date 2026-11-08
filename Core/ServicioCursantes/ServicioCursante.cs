using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes.DTOs.Responses;
using Core.Shared.DTOs.Cursadas;
using Core.ServicioCursantes.Exceptions;
using Core.Shared;
using Core.Shared.Seguridad;
using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using Domain.Cursantes.Exceptions;
using Domain.Shared;
using Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Core.ServicioCursantes;

internal class ServicioCursante : IServicio, IServicioCursante
{
	private readonly ILogger<ServicioCursante> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ISesionActual _sesion;


	public ServicioCursante(ILogger<ServicioCursante> logger, IUnitOfWork unitOfWork, ISesionActual sesion)
	{
		_sesion = sesion;
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	private async Task<Cursante> BuscarCursanteAsync(Guid unCursante)
	{
		var cursante = await _unitOfWork.Cursantes.BuscarPorIDAsync(unCursante);
		if (cursante is null)
		{
			throw new CursanteNoEncontradoException(nameof(unCursante));
		}

		return cursante;
	}

	#region Inscripción
	public async Task InscribirCursanteAsync(RegistrarCursanteRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.InscribirCursantes);

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

			var existeInscripcion = await _unitOfWork.Cursantes.ExisteInscripcionActivaAsync(request.AlumnoID, cicloLectivo);
			if (existeInscripcion)
			{
				throw new InscripcionDuplicadaException();
			}

			var cantidadCursantes = await _unitOfWork.Cursantes.ContarCursantesDeDivisionAsync(division.Id, cicloLectivo);
			division.ValidarCupoDisponible(cantidadCursantes);

			var cursante = new Cursante(division.Id, request.AlumnoID, cicloLectivo, request.FechaInicio, request.EsRecursante);

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

	#region Condición de recursante
	public async Task ModificarCondicionDeRecursanteAsync(ModificarCondicionRecursanteRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.InscribirCursantes);

			var cursante = await BuscarCursanteAsync(request.CursanteID);
			cursante.ModificarCondicionDeRecursante(request.EsRecursante);

			_unitOfWork.Cursantes.Modificar(cursante);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } condición de recursante modificada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Baja
	public async Task RegistrarBajaDeCursanteAsync(RegistrarBajaCursanteRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.InscribirCursantes);

			var cursante = await BuscarCursanteAsync(request.CursanteID);
			cursante.DarDeBaja(request.FechaBaja);

			_unitOfWork.Cursantes.Modificar(cursante);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } baja del cursante registrada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Cambio de división
	/// <summary>
	/// Traslada al cursante a otra división del mismo ciclo lectivo: cierra la inscripción vigente
	/// y abre una nueva en la división destino.
	/// </summary>
	/// <remarks>
	/// Las dos escrituras van en una <strong>sola</strong> unidad de trabajo —un único
	/// <c>GuardarCambiosAsync</c> al final—: si se guardaran por separado, una falla intermedia
	/// dejaría al alumno sin ninguna inscripción activa en el ciclo.
	///
	/// Las calificaciones <strong>no se copian</strong>. Son entidades hijas de la inscripción que
	/// las registró y ahí quedan; el boletín las une leyendo todas las inscripciones del ciclo
	/// (<c>ContextoAcademico.ResolverCursanteAsync</c>).
	/// </remarks>
	public async Task CambiarDeDivisionAsync(CambiarDivisionCursanteRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.InscribirCursantes);

			var cicloLectivo = CicloLectivo.Crear(request.Periodo);

			var inscripcionActual = await _unitOfWork.Cursantes.BuscarInscripcionActivaAsync(request.AlumnoID, cicloLectivo);
			if (inscripcionActual is null)
			{
				throw new NullReferenceException("No se encontró la inscripción del alumno en el ciclo lectivo indicado.");
			}

			var divisionDestino = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionDestinoID);
			if (divisionDestino is null)
			{
				throw new NullReferenceException("La división no existe.");
			}

			if (inscripcionActual.DivisionID.Equals(divisionDestino.Id))
			{
				throw new DivisionDestinoInvalidaException();
			}

			var cantidadCursantes = await _unitOfWork.Cursantes.ContarCursantesDeDivisionAsync(divisionDestino.Id, cicloLectivo);
			divisionDestino.ValidarCupoDisponible(cantidadCursantes);

			inscripcionActual.DarDeBaja(request.FechaCambio);
			_unitOfWork.Cursantes.Modificar(inscripcionActual);

			var inscripcionNueva = new Cursante(
				divisionDestino.Id,
				inscripcionActual.AlumnoID,
				cicloLectivo,
				request.FechaCambio,
				inscripcionActual.EsRecursante);
			await _unitOfWork.Cursantes.AgregarAsync(inscripcionNueva);

			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } cursante trasladado de división correctamente.");
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

	/// <summary>
	/// Historial de divisiones del alumno: todas sus inscripciones, de cualquier ciclo lectivo,
	/// activas y cerradas, ordenadas por fecha de inicio.
	/// </summary>
	/// <remarks>
	/// Es una lectura, así que no lleva guarda de permiso ni <c>GuardarCambiosAsync</c>: es el
	/// patrón vigente de este repositorio de código (CORE-16), no un olvido.
	/// </remarks>
	public async Task<IReadOnlyCollection<InscripcionHistoricaResponse>> ConsultarHistorialDeDivisionesAsync(ConsultarHistorialRequest request)
	{
		try
		{
			var inscripciones = await _unitOfWork.Cursantes.BuscarInscripcionesDelAlumnoAsync(request.AlumnoID);

			var respuesta = new List<InscripcionHistoricaResponse>();
			foreach (var inscripcion in inscripciones)
			{
				var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(inscripcion.DivisionID);
				var curso = division is null ? null : await _unitOfWork.Cursos.BuscarPorIDAsync(division.CursoID);

				respuesta.Add(new InscripcionHistoricaResponse(
					CursanteID: inscripcion.Id,
					DivisionID: inscripcion.DivisionID,
					Division: division?.Descripcion ?? string.Empty,
					Grado: curso?.Grado.ToString() ?? string.Empty,
					NivelEducativo: curso?.NivelEducativo.ToString() ?? string.Empty,
					CicloLectivo: inscripcion.CicloLectivo.Periodo,
					FechaInicio: inscripcion.FechaInicio,
					FechaFin: inscripcion.FechaFin,
					EsRecursante: inscripcion.EsRecursante,
					EstaActiva: inscripcion.EstaActiva()));
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
					Periodo: calificacion.Periodo.ToString(),
					EsRecuperatorio: calificacion.EsRecuperatorio,
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
			_sesion.Exigir(Permiso.CargarCalificaciones);

			var cicloLectivo = CicloLectivo.Crear(request.CicloLectivo);
			var cursante = await _unitOfWork.Cursantes.BuscarInscripcionActivaAsync(request.AlumnoID, cicloLectivo);
			if (cursante is null)
			{
				throw new NullReferenceException("No se encontró la inscripción del alumno en el ciclo lectivo indicado.");
			}

			var resultPeriodo = Enum.TryParse<Periodo>(request.Periodo, true, out var periodo);
			if (!resultPeriodo)
			{
				throw new ArgumentException("El período especificado no existe.", nameof(request.Periodo));
			}

			var calificacionID = cursante.RegistrarCalificacion(request.MateriaID, request.Fecha, periodo, request.EsRecuperatorio, request.Nota, request.Observacion);

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
			_sesion.Exigir(Permiso.CargarCalificaciones);

			var cicloLectivo = CicloLectivo.Crear(request.CicloLectivo);
			var cursante = await _unitOfWork.Cursantes.BuscarInscripcionActivaAsync(request.AlumnoID, cicloLectivo);
			if (cursante is null)
			{
				throw new NullReferenceException("No se encontró la inscripción del alumno en el ciclo lectivo indicado.");
			}

			var resultPeriodo = Enum.TryParse<Periodo>(request.Periodo, true, out var periodo);
			if (!resultPeriodo)
			{
				throw new ArgumentException("El período especificado no existe.", nameof(request.Periodo));
			}

			var calificacionID = cursante.RegistrarInasistenciaAExamen(request.MateriaID, request.Fecha, periodo, request.EsRecuperatorio, request.Observacion);

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
			_sesion.Exigir(Permiso.CargarCalificaciones);

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
			_sesion.Exigir(Permiso.CargarCalificaciones);

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
