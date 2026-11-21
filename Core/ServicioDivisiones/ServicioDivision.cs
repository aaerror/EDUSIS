using Core.ServicioCursos.Exceptions;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Core.ServicioDivisiones.Exceptions;
using Core.Shared;
using Core.Shared.Seguridad;
using Domain.Asistencias;
using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using Domain.Divisiones;
using Domain.Shared;
using Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Core.ServicioDivisiones;

internal class ServicioDivision : IServicio, IServicioDivision
{
	private readonly ILogger<ServicioDivision> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ISesionActual _sesion;


	public ServicioDivision(ILogger<ServicioDivision> logger, IUnitOfWork unitOfWork, ISesionActual sesion)
	{
		_sesion = sesion;
		_logger = logger;
		_unitOfWork = unitOfWork;
	}

	#region Listado
	public async Task<IReadOnlyCollection<DivisionResponse>> ListarDivisionesAsync(ListarDivisionesRequest request)
	{
		try
		{
			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new CursoNoEncontradoException();
			}

			var cicloLectivo = CicloLectivo.Crear(request.CicloLectivo);
			var divisiones = await _unitOfWork.Divisiones.DivisionesDelCursoAsync(request.CursoID);

			var respuesta = new List<DivisionResponse>();
			foreach (var division in divisiones)
			{
				var cantidadCursantes = await _unitOfWork.Cursantes.ContarCursantesDeDivisionAsync(division.Id, cicloLectivo);

				string? preceptor = null;
				if (division.Preceptor is not null)
				{
					var docente = await _unitOfWork.Docentes.BuscarPorIDAsync(division.Preceptor.Value);
					preceptor = docente?.DatosPersonales.NombreCompleto();
				}

				respuesta.Add(new DivisionResponse(
					DivisionID: division.Id,
					Descripcion: division.Descripcion,
					PreceptorID: division.Preceptor,
					Preceptor: preceptor,
					Cursantes: cantidadCursantes));
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

	#region Proponer descripción
	/// <summary>
	/// Letra que el formulario de alta precarga: la que sigue a la última división del curso. El
	/// usuario la puede cambiar, así que es una propuesta y no la descripción definitiva. Es una
	/// <strong>lectura</strong>: sin <c>Exigir</c> y sin <c>GuardarCambiosAsync</c>, porque la
	/// <see cref="Division"/> que construye <c>Division.Siguiente</c> nunca entra al repositorio.
	/// </summary>
	public async Task<string> ProponerDescripcionDivisionAsync(ProponerDescripcionRequest request)
	{
		try
		{
			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new CursoNoEncontradoException();
			}

			var descripciones = await _unitOfWork.Divisiones.DescripcionesDelCursoAsync(request.CursoID);

			return Division.Siguiente(request.CursoID, descripciones).Descripcion;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Agregar, Eliminar
	public async Task AgregarDivisionAsync(AgregarDivisionRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCursos);

			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new CursoNoEncontradoException();
			}

			// La letra la elige el usuario sobre la propuesta de ProponerDescripcionDivisionAsync:
			// el constructor es el que valida que sea una única letra.
			var nuevaDivision = new Division(request.CursoID, request.Descripcion);

			var existeDivision = await _unitOfWork.Divisiones.ExisteDivisionConDescripcionAsync(request.CursoID, nuevaDivision.Descripcion);
			if (existeDivision)
			{
				throw new DivisionDuplicadaException();
			}

			await _unitOfWork.Divisiones.AgregarAsync(nuevaDivision);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } división registrada correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <summary>
	/// Elimina la división y toda la trayectoria que cuelga de ella: cátedras, planillas de
	/// asistencia, inscripciones y calificaciones. <strong>Siempre</strong>: no hay guarda de
	/// matrícula.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Este caso de uso no tiene guarda de negocio por nivel educativo ni guarda de integridad que
	/// rechace el borrado ante una inscripción histórica: se quitaron por decisión de negocio, junto
	/// con el requerimiento que las sostenía (RF-02 y CU-02), para que <em>eliminar</em> signifique
	/// lo mismo en los cinco agregados: borrado físico, con un diálogo que informa qué se destruye.
	/// </para>
	/// <para>
	/// <strong>El orden de los cuatro pasos lo impone el grafo de claves ajenas, no el gusto.</strong>
	/// Las tres que apuntan a <c>division</c> —<c>FK_CATEDRA_DIVISION</c>,
	/// <c>FK_PLANILLA_ASISTENCIA_DIVISION</c> y <c>FK_CURSANTE_DIVISION</c>— son <c>Restrict</c>, así
	/// que la fila de la división es la última que puede caer. Lo que sí cascadea solo son las
	/// entidades hijas: horarios y situaciones de revista por cátedra, registros por planilla, y
	/// calificaciones por cursante.
	/// </para>
	/// <para>
	/// La guarda de autorización corre <strong>antes</strong> de <c>BeginTransactionAsync</c> y el
	/// rollback va en un <c>try</c> propio, con el molde de <c>ServicioAlumno.EliminarAlumnoAsync</c>:
	/// es el tercer caso de uso de <c>Core</c> con transacción explícita y el defecto de hacer
	/// rollback sobre una transacción que nunca se abrió porque la autorización falló podría
	/// reaparecer igual.
	/// </para>
	/// </remarks>
	public async Task EliminarDivisionAsync(EliminarDivisionRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCursos);

			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new CursoNoEncontradoException();
			}

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new DivisionNoEncontradaException();
			}

			await _unitOfWork.BeginTransactionAsync();

			try
			{
				// 1. Las cátedras de la división. Sus horarios y situaciones de revista caen por
				//    FK_HORARIO_CATEDRA y FK_SITUACION_REVISTA_CATEDRA, que sí son Cascade: son
				//    entidades hijas de Catedra y no tienen vida propia.
				var catedras = await _unitOfWork.Catedras.CatedrasSegunDivisionAsync(division.Id);
				_unitOfWork.Catedras.EliminarRango(catedras);
				await _unitOfWork.GuardarCambiosAsync();

				// 2. Las planillas de asistencia. Sus registros caen por
				//    FK_REGISTRO_ASISTENCIA_PLANILLA_ASISTENCIA, también Cascade.
				var planillas = await _unitOfWork.PlanillasAsistencia.BuscarPorDivisionAsync(division.Id);
				_unitOfWork.PlanillasAsistencia.EliminarRango(planillas);
				await _unitOfWork.GuardarCambiosAsync();

				// 3. Las inscripciones, de cualquier ciclo lectivo. Sus calificaciones caen por
				//    FK_CALIFICACION_CURSANTE, Cascade. Es este paso el que vacía la tabla
				//    calificacion y, por lo tanto, lo que después vuelve borrables las materias del
				//    curso: FK_CALIFICACION_MATERIA es Restrict. Ver Curso.ValidarSePuedeEliminar.
				var cursantes = await _unitOfWork.Cursantes.BuscarInscripcionesDeDivisionAsync(division.Id);
				_unitOfWork.Cursantes.EliminarRango(cursantes);
				await _unitOfWork.GuardarCambiosAsync();

				// 4. La división.
				await _unitOfWork.Divisiones.EliminarAsync(division.Id);
				await _unitOfWork.GuardarCambiosAsync();

				await _unitOfWork.CommitTransactionAsync();

				_logger.LogInformation($"División { division.Id } eliminada con { cursantes.Count } inscripciones, { planillas.Count } planillas y { catedras.Count } cátedras.");
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

	/// <summary>
	/// Qué destruiría eliminar esta división. Lo consume el diálogo de confirmación.
	/// </summary>
	/// <remarks>
	/// <strong>Es una lectura con guarda de permiso</strong>, a diferencia de las
	/// ~30 consultas de <c>Core</c> que quedan abiertas. La asimetría es deliberada y es la
	/// misma que ya tiene <c>ServicioAlumno.ConsultarImpactoEliminacionAsync</c>: esta consulta existe
	/// sólo para servir al borrado, lo que devuelve es el volumen de la trayectoria académica de una
	/// división entera, y no la consume ninguna pantalla ajena ni ningún combo — así que no corre el
	/// riesgo que motivó dejar las lecturas abiertas.
	///
	/// Sale de las mismas tres consultas que ejecuta el borrado, no de consultas paralelas.
	/// </remarks>
	public async Task<ImpactoEliminacionDivisionResponse> ConsultarImpactoEliminacionDivisionAsync(EliminarDivisionRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCursos);

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new DivisionNoEncontradaException();
			}

			var catedras = await _unitOfWork.Catedras.CatedrasSegunDivisionAsync(division.Id);
			var planillas = await _unitOfWork.PlanillasAsistencia.BuscarPorDivisionAsync(division.Id);
			var cursantes = await _unitOfWork.Cursantes.BuscarInscripcionesDeDivisionAsync(division.Id);

			return new ImpactoEliminacionDivisionResponse(
				Inscripciones: cursantes.Count,
				Calificaciones: cursantes.Sum(x => x.Calificaciones.Count),
				Planillas: planillas.Count,
				RegistrosAsistencia: planillas.Sum(x => x.Registros.Count),
				Catedras: catedras.Count,
				Designaciones: catedras.Sum(x => x.SituacionesRevista.Count),
				CiclosLectivos: cursantes.Select(x => x.CicloLectivo.Periodo)
										 .Distinct()
										 .OrderBy(x => x)
										 .ToList());
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion

	#region Resumen del ciclo
	/// <summary>
	/// Cómo está la división en el ciclo lectivo pedido: matrícula, estado de hoy, asistencia y
	/// rendimiento. Lo consume la ficha de la división elegida.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Es una consulta y no lleva guarda: lo que devuelve son cifras de una división, sin datos
	/// personales más allá de los nombres de los alumnos con más faltas, que la planilla de
	/// asistencia ya le muestra a cualquiera que la abre.
	/// </para>
	/// <para>
	/// El ciclo no tiene fechas propias —<see cref="CicloLectivo"/> es sólo el año—, así que el
	/// período de la asistencia es el año calendario: del 1 de enero hasta hoy si es el año en
	/// curso, o hasta el 31 de diciembre si ya pasó. Lo que describe el presente y no un ciclo
	/// (planilla de hoy, licencia del preceptor, cátedras) se informa sólo en el año en curso: ver
	/// <see cref="ResumenDivisionResponse"/>.
	/// </para>
	/// </remarks>
	public async Task<ResumenDivisionResponse> ConsultarResumenDivisionAsync(ConsultarResumenDivisionRequest request)
	{
		try
		{
			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new DivisionNoEncontradaException();
			}

			var ciclo = CicloLectivo.Crear(request.CicloLectivo);
			var enCurso = ciclo.AñoEnCurso();

			var inscripciones = await _unitOfWork.Cursantes.BuscarInscripcionesDeDivisionDelCicloAsync(division.Id, ciclo);
			var activas = inscripciones.Where(x => x.EstaActiva()).ToList();

			var año = int.Parse(ciclo.Periodo);
			var desde = new DateTime(año, 1, 1);
			var hasta = enCurso ? Reloj.Hoy : new DateTime(año, 12, 31);
			var planillas = await _unitOfWork.PlanillasAsistencia.BuscarPorDivisionYRangoAsync(division.Id, desde, hasta);
			var registros = planillas.SelectMany(x => x.Registros).ToList();
			var ausencias = registros.Count(x => x.Tipo.Equals(TipoAsistencia.Ausencia));
			var inasistencias = registros.Count(x => x.Tipo.Equals(TipoAsistencia.Inasistencia));

			// Agrupado una sola vez por alumno y por materia: el promedio y las materias desaprobadas
			// tienen que mirar los mismos grupos, como en el resumen académico del alumno.
			var materiasPorAlumno = activas
				.Select(x => x.Calificaciones.GroupBy(c => c.MateriaID).ToList())
				.ToList();

			var hoy = enCurso ? await DescribirEstadoDeHoyAsync(division, planillas) : null;

			return new ResumenDivisionResponse(
				CicloLectivo: ciclo.Periodo,
				EsCicloEnCurso: enCurso,
				Inscriptos: activas.Count,
				Cupo: Division.MAX_ALUMNOS,
				Recursantes: activas.Count(x => x.EsRecursante),
				Bajas: inscripciones.Count - activas.Count,
				PlanillaDeHoy: hoy?.Planilla,
				PreceptorDeLicencia: hoy?.PreceptorDeLicencia,
				CatedrasSinDocente: hoy?.CatedrasSinDocente,
				CatedrasConSuplente: hoy?.CatedrasConSuplente,
				HorasSinAsignar: hoy?.HorasSinAsignar,
				MateriasSinCatedra: hoy?.MateriasSinCatedra,
				Planillas: planillas.Count,
				// "Sin cerrar" son las de días anteriores: la de hoy abierta es la jornada en curso, y
				// ya la informa PlanillaDeHoy.
				PlanillasSinCerrar: planillas.Count(x => !x.Cerrada && x.Fecha.Date < Reloj.Hoy),
				PorcentajeAsistencia: registros.Count is 0
					? null
					: PorcentajeDeAsistencia.Calcular(asistidas: registros.Count - ausencias - inasistencias, sobre: registros.Count),
				Faltas: ausencias + inasistencias,
				Tardanzas: registros.Count(x => x.Tipo.Equals(TipoAsistencia.Tardanza)),
				AlumnosConMasFaltas: await DescribirAlumnosConMasFaltasAsync(activas, registros),
				Promedio: RendimientoAcademico.PromediarAlumnos(materiasPorAlumno),
				// Mismo criterio que MateriasDesaprobadas en el perfil del alumno: una materia con
				// calificaciones y sin cierre aprobado. A mitad de año eso incluye a quien va
				// desaprobando el primer cuatrimestre, que es justamente a quien hay que mirar.
				AlumnosConMateriasDesaprobadas: materiasPorAlumno.Count(materias => materias.Any(m => !RendimientoAcademico.EstaAprobada(m))),
				AusenciasAExamen: RendimientoAcademico.AusenciasAExamen(activas.SelectMany(x => x.Calificaciones)).Count());
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	private sealed record EstadoDeHoy(
		EstadoPlanillaDelDia Planilla,
		bool PreceptorDeLicencia,
		int CatedrasSinDocente,
		int CatedrasConSuplente,
		int HorasSinAsignar,
		IReadOnlyCollection<string> MateriasSinCatedra);

	private async Task<EstadoDeHoy> DescribirEstadoDeHoyAsync(Division division, IReadOnlyCollection<PlanillaAsistencia> planillas)
	{
		var deHoy = planillas.FirstOrDefault(x => x.Fecha.Date == Reloj.Hoy);
		var planilla = deHoy is null
			? EstadoPlanillaDelDia.SinAbrir
			: deHoy.Cerrada ? EstadoPlanillaDelDia.Cerrada : EstadoPlanillaDelDia.Abierta;

		var preceptorDeLicencia = false;
		if (division.Preceptor is not null)
		{
			var licencias = await _unitOfWork.Licencias.BuscarLicenciasDeDocenteAsync(division.Preceptor.Value);
			preceptorDeLicencia = licencias.Any(x => x.EstaActiva());
		}

		var catedras = await _unitOfWork.Catedras.CatedrasSegunDivisionAsync(division.Id);

		// Con suplente: quien está en funciones reemplaza a otro. Se lee de la designación en
		// funciones y no de "hay alguna suplencia vigente", porque la cátedra puede tener al
		// suplente designado y al titular ya repuesto tras la licencia.
		var conSuplente = catedras.Count(catedra =>
			catedra.SituacionEnFuncionesID is Guid enFunciones
			&& catedra.SituacionesRevista.Any(x => x.Id.Equals(enFunciones) && x.ReemplazaA is not null));

		// Las materias salen de la currícula vigente del curso. Sin currícula vigente no hay plan del
		// que faltar materias, y la lista queda vacía en lugar de inventar una.
		var curriculas = await _unitOfWork.Curriculas.CurriculasSegunCursoAsync(division.CursoID);
		var vigente = curriculas.FirstOrDefault(x => x.EstaVigente());
		var materiasSinCatedra = new List<string>();
		if (vigente is not null)
		{
			var materias = await _unitOfWork.Materias.BuscarMateriasSegunCurriculaAsync(vigente.Id);
			materiasSinCatedra = materias
				.Where(materia => !catedras.Any(catedra => catedra.MateriaID.Equals(materia.Id)))
				.Select(materia => materia.Descripcion)
				.OrderBy(x => x)
				.ToList();
		}

		return new EstadoDeHoy(
			Planilla: planilla,
			PreceptorDeLicencia: preceptorDeLicencia,
			CatedrasSinDocente: catedras.Count(x => x.SituacionEnFuncionesID is null),
			CatedrasConSuplente: conSuplente,
			HorasSinAsignar: catedras.Sum(x => x.HorasSinAsignar),
			MateriasSinCatedra: materiasSinCatedra);
	}

	/// <summary>
	/// Los tres alumnos de la matrícula vigente con más faltas en el período, sólo si faltaron.
	/// </summary>
	/// <remarks>
	/// Mismo orden que <c>ServicioAsistencia.ConsultarResumenAsync</c>: más faltas primero y, a igual
	/// cantidad, el que menos jornadas tuvo, porque las mismas faltas sobre menos días pesan más. Los
	/// dados de baja quedan afuera: el resumen habla de quién está hoy en la división.
	/// </remarks>
	private async Task<IReadOnlyCollection<AlumnoConFaltasResponse>> DescribirAlumnosConMasFaltasAsync(
		IReadOnlyCollection<Cursante> activas,
		IReadOnlyCollection<RegistroAsistencia> registros)
	{
		const int CANTIDAD = 3;

		var alumnoDe = activas.ToDictionary(x => x.Id, x => x.AlumnoID);

		var conMasFaltas = registros
			.Where(x => alumnoDe.ContainsKey(x.CursanteID))
			.GroupBy(x => x.CursanteID)
			.Select(grupo => new
			{
				AlumnoID = alumnoDe[grupo.Key],
				Jornadas = grupo.Count(),
				Faltas = grupo.Count(x => x.Tipo.Equals(TipoAsistencia.Ausencia) || x.Tipo.Equals(TipoAsistencia.Inasistencia))
			})
			.Where(x => x.Faltas > 0)
			.OrderByDescending(x => x.Faltas)
			.ThenBy(x => x.Jornadas)
			.Take(CANTIDAD)
			.ToList();

		var respuesta = new List<AlumnoConFaltasResponse>();
		foreach (var item in conMasFaltas)
		{
			var alumno = await _unitOfWork.Alumnos.BuscarPorIDAsync(item.AlumnoID);

			respuesta.Add(new AlumnoConFaltasResponse(
				AlumnoID: item.AlumnoID,
				NombreCompleto: alumno?.DatosPersonales.NombreCompleto() ?? "Alumno inexistente",
				Faltas: item.Faltas,
				PorcentajeAsistencia: PorcentajeDeAsistencia.Calcular(asistidas: item.Jornadas - item.Faltas, sobre: item.Jornadas)));
		}

		return respuesta.AsReadOnly();
	}
	#endregion

	// Las tres operaciones de preceptor exigen GestionarPreceptores y no GestionarCursos: el
	// diagrama de casos de uso de Secretaría le da CU-04/05/06 al Encargado de Secretaría, mientras
	// que el alta y la baja de divisiones —acá arriba, con GestionarCursos— son de Secretaría
	// Académica. Eran el mismo permiso y concedían los dos grupos juntos.
	#region Preceptor
	public async Task AsignarPreceptorAsync(RegistrarPreceptorRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarPreceptores);

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new DivisionNoEncontradaException();
			}

			var existePreceptor = await _unitOfWork.Divisiones.ExistePreceptorAsignadoAsync(request.DocenteID);
			if (existePreceptor)
			{
				throw new PreceptorAsignadoException();
			}

			division.AsignarPreceptor(request.DocenteID);

			_unitOfWork.Divisiones.Modificar(division);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } preceptor asignado correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task ModificarPreceptorAsync(ModificarPreceptorRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarPreceptores);

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new DivisionNoEncontradaException();
			}

			var existePreceptor = await _unitOfWork.Divisiones.ExistePreceptorAsignadoAsync(request.DocenteID);
			if (existePreceptor)
			{
				throw new PreceptorAsignadoException();
			}

			division.CambiarPreceptor(request.DocenteID);

			_unitOfWork.Divisiones.Modificar(division);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } preceptor modificado correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	public async Task QuitarPreceptorAsync(EliminarPreceptorRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarPreceptores);

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new DivisionNoEncontradaException();
			}

			division.QuitarPreceptor();

			_unitOfWork.Divisiones.Modificar(division);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } preceptor removido correctamente.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
