using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Core.ServicioDivisiones.Exceptions;
using Core.Shared;
using Core.Shared.Seguridad;
using Domain.Cursantes;
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
				throw new InvalidOperationException("El curso no existe.");
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
				throw new InvalidOperationException("El curso no existe.");
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
				throw new InvalidOperationException("El curso no existe.");
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
	/// Hasta el 2026-11-06 este caso de uso tenía dos guardas y ninguna cascada: una regla de
	/// negocio por nivel educativo (<c>Division.ValidarSePuedeEliminar</c>) y una guarda de
	/// integridad que rechazaba el borrado ante una sola inscripción histórica. Las dos se quitaron
	/// por decisión de negocio, junto con el requerimiento que las sostenía (RF-02 y CU-S11 en
	/// <c>docs/uml/</c>), para que <em>eliminar</em> signifique lo mismo en los cinco agregados:
	/// borrado físico, con un diálogo que informa qué se destruye.
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
	/// es el tercer caso de uso de <c>Core</c> con transacción explícita y el defecto de CORE-19
	/// —rollback sobre una transacción que nunca se abrió porque la autorización falló— podía
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
				throw new InvalidOperationException("El curso no existe.");
			}

			var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(request.DivisionID);
			if (division is null)
			{
				throw new NullReferenceException("La división no existe.");
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
	/// <strong>Es una lectura con guarda de permiso</strong>, contra la decisión de alcance de
	/// CORE-16 que dejó las ~30 consultas de <c>Core</c> abiertas. La asimetría es deliberada y es la
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
				throw new NullReferenceException("La división no existe.");
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

	// Las tres operaciones de preceptor exigen GestionarPreceptores y no GestionarCursos: el
	// diagrama de Secretaría (docs/uml/) le da CU-04/05/06 al Encargado de Secretaría, mientras
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
				throw new NullReferenceException("La división no existe.");
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
				throw new NullReferenceException("La división no existe.");
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
				throw new NullReferenceException("La división no existe.");
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
