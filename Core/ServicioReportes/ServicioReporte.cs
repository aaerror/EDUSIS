using Core.ServicioReportes.DTOs.Requests;
using Core.Shared;
using Core.Shared.Documentos;
using Core.Shared.Seguridad;
using Domain.Alumnos;
using Domain.Asistencias;
using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using Domain.Cursos;
using Domain.Divisiones;
using Domain.Materias;
using Domain.Shared;
using Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Core.ServicioReportes;

internal class ServicioReporte : IServicio, IServicioReporte
{
	private readonly ILogger<ServicioReporte> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IGeneradorDocumentos _generadorDocumentos;
	private readonly IDatosInstitucionales _institucion;
	private readonly ISesionActual _sesion;

	private readonly ContextoAcademico _contexto;


	public ServicioReporte(ILogger<ServicioReporte> logger,
						   IUnitOfWork unitOfWork,
						   IGeneradorDocumentos generadorDocumentos,
						   IDatosInstitucionales institucion,
						   ISesionActual sesion)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
		_generadorDocumentos = generadorDocumentos;
		_institucion = institucion;
		_sesion = sesion;

		_contexto = new ContextoAcademico(unitOfWork);
	}

	/// <summary>Una inscripción con su alumno, división y curso ya resueltos.</summary>
	private sealed record Inscripcion(Cursante Cursante, Alumno Alumno, Division Division, Curso Curso);

	#region Matrículas
	public async Task<DocumentoGenerado> GenerarReporteMatriculasAsync(ReportePorCicloRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GenerarReportes);

			var inscripciones = await ListarInscripcionesAsync(request.Periodo, request.DivisionID);

			var filas = inscripciones
				.Select(x => (IReadOnlyCollection<string>) new[]
					{
						x.Curso.NivelEducativo.ToString(),
						ContextoAcademico.Describir(x.Curso.Grado),
						x.Division.Descripcion,
						x.Alumno.Legajo,
						x.Alumno.DatosPersonales.NombreCompleto(),
						x.Alumno.DatosPersonales.Documento,
						x.Cursante.FechaInicio.ToString("dd/MM/yyyy"),
						x.Cursante.EsRecursante ? "Recursante" : "Regular"
					})
				.ToList();

			return await GenerarAsync(
				titulo: "Reporte de matrículas",
				request: request,
				columnas: new[] { "Nivel", "Curso", "Div.", "Legajo", "Alumno", "Documento", "Inscripción", "Condición" },
				filas: filas,
				inscripciones: inscripciones);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Asistencias
	public async Task<DocumentoGenerado> GenerarReporteAsistenciasAsync(ReporteAsistenciasRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GenerarReportes);

			if (request.Desde.Date > request.Hasta.Date)
			{
				throw new ArgumentException("El inicio del período es posterior a su fin.", nameof(request.Desde));
			}

			var inscripciones = await ListarInscripcionesAsync(request.Periodo, request.DivisionID);

			var planillas = request.DivisionID.HasValue
				? await _unitOfWork.PlanillasAsistencia.BuscarPorDivisionYRangoAsync(request.DivisionID.Value, request.Desde, request.Hasta)
				: await _unitOfWork.PlanillasAsistencia.BuscarPorRangoAsync(request.Desde, request.Hasta);

			// Los registros se indexan por cursante una sola vez: recorrer todas las planillas por
			// cada alumno sería cuadrático sobre la matrícula del período.
			var registrosPorCursante = planillas
				.SelectMany(planilla => planilla.Registros)
				.GroupBy(registro => registro.CursanteID)
				.ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());

			var filas = inscripciones
				.Select(x =>
					{
						registrosPorCursante.TryGetValue(x.Cursante.Id, out var registros);
						registros ??= new List<RegistroAsistencia>();

						var computadas = registros.Count;
						var presentes = registros.Count(r => r.Tipo == TipoAsistencia.Presente);
						var tardanzas = registros.Count(r => r.Tipo == TipoAsistencia.Tardanza);

						// La tardanza es concurrencia: el alumno estuvo. Contarla como ausencia
						// castigaría dos veces el mismo hecho.
						var concurrio = presentes + tardanzas;

						return (IReadOnlyCollection<string>) new[]
							{
								ContextoAcademico.Describir(x.Curso.Grado),
								x.Division.Descripcion,
								x.Alumno.Legajo,
								x.Alumno.DatosPersonales.NombreCompleto(),
								computadas.ToString(),
								presentes.ToString(),
								registros.Count(r => r.Tipo == TipoAsistencia.Ausencia).ToString(),
								registros.Count(r => r.Tipo == TipoAsistencia.Inasistencia).ToString(),
								tardanzas.ToString(),
								computadas == 0 ? ContextoAcademico.SIN_DATO : $"{ (double) concurrio / computadas:P1}"
							};
					})
				.ToList();

			var documento = new ReporteTabularDocumento(
				Titulo: "Reporte de asistencias",
				Alcance: $"{ DescribirAlcance(request.Periodo, request.DivisionID, inscripciones) } · Período del {request.Desde:dd/MM/yyyy} al {request.Hasta:dd/MM/yyyy}",
				Columnas: new[] { "Curso", "Div.", "Legajo", "Alumno", "Computadas", "Presentes", "Ausencias", "Inasist.", "Tardanzas", "Asistencia" },
				Filas: filas.AsReadOnly(),
				Emision: Emitir());

			var generado = await _generadorDocumentos.GenerarReporteAsync(documento);

			_logger.LogInformation($"Reporte de asistencias emitido con { filas.Count } filas.");

			return generado;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Calificaciones
	public async Task<DocumentoGenerado> GenerarReporteCalificacionesAsync(ReportePorCicloRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GenerarReportes);

			var inscripciones = await ListarInscripcionesAsync(request.Periodo, request.DivisionID);
			var materiasPorCurso = await MateriasPorCursoAsync(inscripciones);

			var filas = new List<IReadOnlyCollection<string>>();
			foreach (var inscripcion in inscripciones)
			{
				foreach (var materia in materiasPorCurso[inscripcion.Curso.Id].OrderBy(x => x.Descripcion))
				{
					filas.Add(new[]
						{
							ContextoAcademico.Describir(inscripcion.Curso.Grado),
							inscripcion.Division.Descripcion,
							inscripcion.Alumno.DatosPersonales.NombreCompleto(),
							materia.Descripcion,
							ContextoAcademico.DescribirNota(inscripcion.Cursante, materia.Id, Instancia.Parcial),
							ContextoAcademico.DescribirNota(inscripcion.Cursante, materia.Id, Instancia.Recuperatorio),
							ContextoAcademico.DescribirCondicion(inscripcion.Cursante, materia.Id, conNota: false)
						});
				}
			}

			return await GenerarAsync(
				titulo: "Reporte de calificaciones",
				request: request,
				columnas: new[] { "Curso", "Div.", "Alumno", "Materia", "Parcial", "Recuperatorio", "Condición" },
				filas: filas,
				inscripciones: inscripciones);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Materias pendientes
	public async Task<DocumentoGenerado> GenerarReporteMateriasPendientesAsync(ReportePorCicloRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GenerarReportes);

			var inscripciones = await ListarInscripcionesAsync(request.Periodo, request.DivisionID);
			var materiasPorCurso = await MateriasPorCursoAsync(inscripciones);

			var filas = new List<IReadOnlyCollection<string>>();
			foreach (var inscripcion in inscripciones)
			{
				var pendientes = materiasPorCurso[inscripcion.Curso.Id]
					.Where(materia => !ContextoAcademico.EstaAprobada(inscripcion.Cursante, materia.Id))
					.OrderBy(materia => materia.Descripcion);

				foreach (var materia in pendientes)
				{
					filas.Add(new[]
						{
							ContextoAcademico.Describir(inscripcion.Curso.Grado),
							inscripcion.Division.Descripcion,
							inscripcion.Alumno.Legajo,
							inscripcion.Alumno.DatosPersonales.NombreCompleto(),
							materia.Descripcion,
							materia.HorasCatedra.ToString(),
							ContextoAcademico.DescribirCondicion(inscripcion.Cursante, materia.Id, conNota: false)
						});
				}
			}

			return await GenerarAsync(
				titulo: "Reporte de materias pendientes",
				request: request,
				columnas: new[] { "Curso", "Div.", "Legajo", "Alumno", "Materia", "Hs. cát.", "Condición" },
				filas: filas,
				inscripciones: inscripciones);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Trayectorias académicas
	public async Task<DocumentoGenerado> GenerarReporteTrayectoriasAsync(ReportePorCicloRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GenerarReportes);

			var inscripciones = await ListarInscripcionesAsync(request.Periodo, request.DivisionID);
			var materiasPorCurso = await MateriasPorCursoAsync(inscripciones);

			var filas = inscripciones
				.Select(x =>
					{
						var materias = materiasPorCurso[x.Curso.Id];
						var aprobadas = materias.Count(materia => ContextoAcademico.EstaAprobada(x.Cursante, materia.Id));
						var total = materias.Count;

						return (IReadOnlyCollection<string>) new[]
							{
								ContextoAcademico.Describir(x.Curso.Grado),
								x.Division.Descripcion,
								x.Alumno.Legajo,
								x.Alumno.DatosPersonales.NombreCompleto(),
								x.Cursante.EsRecursante ? "Sí" : "No",
								total.ToString(),
								aprobadas.ToString(),
								(total - aprobadas).ToString(),
								total == 0 ? ContextoAcademico.SIN_DATO : $"{ (double) aprobadas / total:P0}"
							};
					})
				.ToList();

			return await GenerarAsync(
				titulo: "Reporte de trayectorias académicas",
				request: request,
				columnas: new[] { "Curso", "Div.", "Legajo", "Alumno", "Recursa", "Materias", "Aprobadas", "Pendientes", "Avance" },
				filas: filas,
				inscripciones: inscripciones);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Armado
	private async Task<DocumentoGenerado> GenerarAsync(
		string titulo,
		ReportePorCicloRequest request,
		IReadOnlyCollection<string> columnas,
		IReadOnlyCollection<IReadOnlyCollection<string>> filas,
		// Obligatorio y no opcional: de él sale el curso y la división que el reporte declara en
		// su alcance, y omitirlo hacía que un reporte acotado dijera "División seleccionada" en
		// lugar de nombrarla.
		IReadOnlyList<Inscripcion> inscripciones)
	{
		var documento = new ReporteTabularDocumento(
			Titulo: titulo,
			Alcance: DescribirAlcance(request.Periodo, request.DivisionID, inscripciones),
			Columnas: columnas,
			Filas: filas,
			Emision: Emitir());

		var generado = await _generadorDocumentos.GenerarReporteAsync(documento);

		_logger.LogInformation($"{ titulo } emitido con { filas.Count } filas.");

		return generado;
	}

	/// <summary>
	/// Qué abarca el reporte, para que el impreso lo diga en lugar de dejarlo a la memoria de
	/// quien lo emitió.
	/// </summary>
	private static string DescribirAlcance(string periodo, Guid? divisionID, IReadOnlyList<Inscripcion> inscripciones)
	{
		var alcance = $"Ciclo lectivo { periodo }";

		if (!divisionID.HasValue)
		{
			return $"{ alcance } · Institución completa";
		}

		var division = inscripciones.FirstOrDefault()?.Division;
		var curso = inscripciones.FirstOrDefault()?.Curso;

		return division is null || curso is null
			? $"{ alcance } · División seleccionada"
			: $"{ alcance } · { ContextoAcademico.Describir(curso.Grado) } { division.Descripcion }";
	}

	private MetadatosEmision Emitir() =>
		new(Localidad: _institucion.Localidad, Fecha: DateTime.Today, Finalidad: null);
	#endregion

	#region Resolución
	/// <summary>
	/// Inscripciones del ciclo lectivo —de una división, o de toda la institución— con su alumno,
	/// división y curso resueltos, ordenadas como se leen en el reporte.
	/// </summary>
	/// <remarks>
	/// Los catálogos se traen enteros y se indexan en memoria en lugar de consultarse por
	/// inscripción: la matrícula de un ciclo lectivo tiene cientos de filas, y una consulta por
	/// alumno, división y curso de cada una es el N+1 clásico. Alumnos, divisiones y cursos de un
	/// establecimiento son conjuntos chicos y acotados.
	/// </remarks>
	private async Task<IReadOnlyList<Inscripcion>> ListarInscripcionesAsync(string periodo, Guid? divisionID)
	{
		var cicloLectivo = CicloLectivo.Crear(periodo);

		var cursantes = divisionID.HasValue
			? await _unitOfWork.Cursantes.BuscarPorDivisionAsync(divisionID.Value, cicloLectivo)
			: await _unitOfWork.Cursantes.BuscarPorCicloLectivoAsync(cicloLectivo);

		if (!cursantes.Any())
		{
			throw new InvalidOperationException($"No hay inscripciones registradas para el alcance solicitado en el ciclo lectivo { periodo }.");
		}

		var alumnos = (await _unitOfWork.Alumnos.BuscarTodosAsync()).ToDictionary(x => x.Id);
		var divisiones = (await _unitOfWork.Divisiones.BuscarTodosAsync()).ToDictionary(x => x.Id);
		var cursos = (await _unitOfWork.Cursos.BuscarTodosAsync()).ToDictionary(x => x.Id);

		var inscripciones = new List<Inscripcion>();
		foreach (var cursante in cursantes)
		{
			// Una inscripción cuyo alumno, división o curso no se puede resolver se omite en lugar
			// de cortar el reporte: un dato huérfano no debe impedir ver los otros trescientos.
			if (!alumnos.TryGetValue(cursante.AlumnoID, out var alumno) ||
				!divisiones.TryGetValue(cursante.DivisionID, out var division) ||
				!cursos.TryGetValue(division.CursoID, out var curso))
			{
				_logger.LogWarning($"Inscripción { cursante.Id } omitida del reporte: no se pudieron resolver sus datos académicos.");
				continue;
			}

			inscripciones.Add(new Inscripcion(cursante, alumno, division, curso));
		}

		return inscripciones
			.OrderBy(x => x.Curso.NivelEducativo)
			.ThenBy(x => x.Curso.Grado)
			.ThenBy(x => x.Division.Descripcion)
			.ThenBy(x => x.Alumno.DatosPersonales.NombreCompleto())
			.ToList();
	}

	/// <summary>Materias de la currícula vigente de cada curso presente en las inscripciones.</summary>
	private async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Materia>>> MateriasPorCursoAsync(IReadOnlyList<Inscripcion> inscripciones)
	{
		var materias = new Dictionary<Guid, IReadOnlyCollection<Materia>>();

		foreach (var cursoID in inscripciones.Select(x => x.Curso.Id).Distinct())
		{
			materias[cursoID] = await _contexto.MateriasVigentesDelCursoAsync(cursoID);
		}

		return materias;
	}
	#endregion

}
