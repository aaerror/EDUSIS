using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioCursos.Exceptions;
using Core.Shared;
using Core.Shared.Seguridad;
using Domain.Cursos;
using Domain.Shared;
using Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Core.ServicioCursos;

internal class ServicioCurso : IServicio, IServicioCurso
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<ServicioCurso> _logger;
	private readonly ISesionActual _sesion;


	public ServicioCurso(IUnitOfWork unitOfWork, ILogger<ServicioCurso> logger, ISesionActual sesion)
	{
		_sesion = sesion;
		_unitOfWork = unitOfWork;
		_logger = logger;
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
						NivelEducativo: x.NivelEducativo))
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
			_sesion.Exigir(Permiso.GestionarCursos);

			var resultG = Enum.TryParse<Grado>(request.Grado, true, out Grado grado);
			var resultNE = Enum.TryParse<NivelEducativo>(request.NivelEducativo, true, out NivelEducativo nivelEducativo);
			if (resultG && resultNE)
			{
				var existeCurso = await _unitOfWork.Cursos.ExisteCursoAsync(grado, nivelEducativo);
				if (existeCurso)
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

	/// <summary>
	/// Elimina el curso y su currícula: cada diseño curricular y las materias de cada uno. Exige que
	/// el curso <strong>no tenga ninguna división</strong>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Reemplaza un <c>EliminarAsync(id)</c> pelado que tenía al lado un
	/// <c>TODO: Eliminar todas las materias y divisiones con este curso asociado</c> admitiendo el
	/// agujero: <c>FK_DIVISION_CURSO</c> y <c>FK_DISENO_CURRICULAR_CURSO</c> son <c>Restrict</c>, así
	/// que el borrado de cualquier curso real fallaba en SQL Server y llegaba al ViewModel como
	/// <c>DbUpdateException</c>. Mismo defecto que tenía alumno antes de CORE-23.
	/// </para>
	/// <para>
	/// <strong>Este caso de uso NO es atómico, y es una decisión.</strong> El cascadeo de materias se
	/// apoya en el despacho de eventos de <c>GuardarCambiosAsync</c>: <c>Materia.Eliminar()</c> encola
	/// <c>MateriaEliminadaEvent</c>, el guardado lo publica —los eventos se despachan ANTES de
	/// <c>SaveChangesAsync</c>— y <c>MateriaEliminadaEventHandler</c> limpia las cátedras huérfanas
	/// con la materia todavía viva. Es el molde que <c>ServicioMateria.EliminarMateriaAsync</c> ya
	/// documenta, y ahí está escrito que son dos transacciones por agregado a propósito: consistencia
	/// eventual, no atomicidad. Envolver todo esto en una transacción propia no arreglaría nada,
	/// porque el handler guarda por su cuenta.
	/// </para>
	/// <para>
	/// Consecuencia asumida: si el borrado falla a mitad de camino, el curso queda con menos materias
	/// de las que tenía. Es un estado válido en la base —nadie referencia una fila inexistente— pero
	/// incompleto respecto de la intención, y sin compensación automática: reintentar el borrado lo
	/// cierra. La alternativa descartada era duplicar la limpieza de cátedras acá dentro para ganar
	/// atomicidad, que dejaría dos caminos para la misma regla.
	/// </para>
	/// </remarks>
	public async Task EliminarCurso(EliminarCursoRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCursos);

			// InvalidOperationException y no una CursoNoEncontradoException nueva: ServicioDivision
			// señala "el curso no existe" exactamente así, y declarar la excepción en Domain para
			// lanzarla sólo desde Core profundizaría DOM-25 (seis excepciones ya están en esa
			// situación). La inconsistencia global de cómo se señala "no encontrado" está registrada
			// en CORE-20 y se resuelve de una vez, no de a un caso de uso.
			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new InvalidOperationException("El curso no existe.");
			}

			// La guarda es lo que vuelve posible la cascada de abajo, no un trámite: sin divisiones no
			// quedan cursantes, y sin cursantes no quedan calificaciones trabando el borrado de las
			// materias por FK_CALIFICACION_MATERIA, que es Restrict. Ver Curso.ValidarSePuedeEliminar.
			var divisiones = await _unitOfWork.Divisiones.DivisionesDelCursoAsync(curso.Id);
			curso.ValidarSePuedeEliminar(divisiones.Count);

			var curriculas = await _unitOfWork.Curriculas.CurriculasSegunCursoAsync(curso.Id);

			var materiasEliminadas = 0;
			foreach (var curricula in curriculas)
			{
				var materias = await _unitOfWork.Materias.BuscarMateriasSegunCurriculaAsync(curricula.Id);

				foreach (var materia in materias)
				{
					// Dos guardados por materia, y el orden NO es decorativo: el primero despacha
					// MateriaEliminadaEvent y deja que el handler borre las cátedras con la materia
					// todavía viva. Marcar el borrado antes rompe la relación requerida Materia→Catedra
					// y EF aborta con "The association between entity types 'Materia' and 'Catedra' has
					// been severed". Es el mismo par de llamadas de ServicioMateria.EliminarMateriaAsync.
					materia.Eliminar();
					await _unitOfWork.GuardarCambiosAsync();

					await _unitOfWork.Materias.EliminarAsync(materia.Id);
					await _unitOfWork.GuardarCambiosAsync();

					materiasEliminadas++;
				}
			}

			_unitOfWork.Curriculas.EliminarRango(curriculas);
			await _unitOfWork.GuardarCambiosAsync();

			await _unitOfWork.Cursos.EliminarAsync(curso.Id);
			var result = await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"{ result } curso eliminado con { curriculas.Count } currículas y { materiasEliminadas } materias.");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}

	/// <summary>
	/// Qué destruiría eliminar este curso, y si se puede eliminar en absoluto. Lo consume el diálogo
	/// de confirmación.
	/// </summary>
	/// <remarks>
	/// Lectura <strong>con</strong> guarda de permiso, misma asimetría deliberada que
	/// <c>ServicioAlumno.ConsultarImpactoEliminacionAsync</c> y
	/// <c>ServicioDivision.ConsultarImpactoEliminacionDivisionAsync</c> frente a la decisión de
	/// alcance de CORE-16: existe sólo para servir al borrado y no la consume ninguna pantalla ajena.
	///
	/// Informa <c>Divisiones</c> aunque el borrado no las toque: es el motivo por el que el caso de
	/// uso va a rechazar, y la pantalla necesita poder decir cuántas hay que eliminar antes en lugar
	/// de ofrecer una confirmación condenada a fallar.
	/// </remarks>
	public async Task<ImpactoEliminacionCursoResponse> ConsultarImpactoEliminacionCursoAsync(EliminarCursoRequest request)
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarCursos);

			var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(request.CursoID);
			if (curso is null)
			{
				throw new InvalidOperationException("El curso no existe.");
			}

			var divisiones = await _unitOfWork.Divisiones.DivisionesDelCursoAsync(curso.Id);
			var curriculas = await _unitOfWork.Curriculas.CurriculasSegunCursoAsync(curso.Id);

			var materias = 0;
			var catedras = 0;
			foreach (var curricula in curriculas)
			{
				var materiasDeLaCurricula = await _unitOfWork.Materias.BuscarMateriasSegunCurriculaAsync(curricula.Id);
				materias += materiasDeLaCurricula.Count;

				foreach (var materia in materiasDeLaCurricula)
				{
					var catedrasDeLaMateria = await _unitOfWork.Catedras.CatedrasSegunMateriaAsync(materia.Id);
					catedras += catedrasDeLaMateria.Count;
				}
			}

			return new ImpactoEliminacionCursoResponse(
				Curriculas: curriculas.Count,
				Materias: materias,
				Catedras: catedras,
				Divisiones: divisiones.Count);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: { ex.Message }\n");
			throw;
		}
	}
	#endregion
}
