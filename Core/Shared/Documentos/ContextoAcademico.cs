using System.ComponentModel;
using System.Reflection;
using Domain.Alumnos;
using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using Domain.Cursos;
using Domain.Divisiones;
using Domain.Materias;
using Domain.Shared;

namespace Core.Shared.Documentos;

/// <summary>
/// Inscripción de un alumno con todo lo que hace falta para identificarlo en un documento.
/// </summary>
/// <param name="CalificacionesDelCiclo">
/// Calificaciones de <strong>todas</strong> las inscripciones del alumno en el ciclo lectivo, no
/// sólo de la activa. Existe porque <c>Calificacion</c> es entidad hija de <see cref="Cursante"/>
/// y el traslado de división cierra una inscripción y abre otra: sin unirlas, el boletín de un
/// alumno trasladado perdería todas las notas anteriores al traslado.
/// </param>
internal sealed record ContextoCursante(
	Alumno Alumno,
	Cursante Cursante,
	Division Division,
	Curso Curso,
	IReadOnlyCollection<Calificacion> CalificacionesDelCiclo);

/// <summary>
/// Resuelve el contexto académico que necesitan los documentos y los reportes: la inscripción de
/// un alumno en un ciclo lectivo y las materias de la currícula vigente de su curso.
/// </summary>
/// <remarks>
/// Está acá, y no duplicado en cada servicio, porque emitir un certificado y armar un reporte
/// parten del mismo recorrido —cursante → división → curso → currícula vigente → materias— y
/// tienen que coincidir: un boletín y el reporte de calificaciones del mismo alumno no pueden
/// discrepar en qué materias le corresponden.
/// </remarks>
internal sealed class ContextoAcademico
{
	/// <summary>Celda sin dato: no hay calificación registrada en esa instancia.</summary>
	public const string SIN_DATO = "—";

	/// <summary>Se registró inasistencia al examen: hay registro, pero no hay nota.</summary>
	public const string NO_RINDIO = "No rindió";

	/// <summary>No hay ninguna calificación de la materia.</summary>
	public const string SIN_CALIFICAR = "Sin calificar";

	private readonly IUnitOfWork _unitOfWork;


	public ContextoAcademico(IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Inscripción activa del alumno en el ciclo lectivo, con su división y curso, más las
	/// calificaciones de todas sus inscripciones del ciclo.
	/// </summary>
	/// <remarks>
	/// <see cref="ContextoCursante.Cursante"/>, <see cref="ContextoCursante.Division"/> y
	/// <see cref="ContextoCursante.Curso"/> son los de la inscripción <strong>activa</strong>: el
	/// encabezado del documento tiene que decir la división actual del alumno. Lo único que se une
	/// de las inscripciones cerradas son las calificaciones.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// El alumno no existe, no está inscripto en el ciclo lectivo, o su división o curso no se
	/// pueden resolver. No se emite documentación a partir de datos incompletos: un certificado
	/// que omite el curso no sirve para el trámite.
	/// </exception>
	public async Task<ContextoCursante> ResolverCursanteAsync(Guid alumnoID, CicloLectivo cicloLectivo)
	{
		var alumno = await _unitOfWork.Alumnos.BuscarPorIDAsync(alumnoID)
			?? throw new InvalidOperationException("El alumno no existe.");

		var cursante = await _unitOfWork.Cursantes.BuscarInscripcionActivaAsync(alumnoID, cicloLectivo)
			?? throw new InvalidOperationException($"El alumno no tiene una inscripción activa en el ciclo lectivo {cicloLectivo.Periodo}.");

		var division = await _unitOfWork.Divisiones.BuscarPorIDAsync(cursante.DivisionID)
			?? throw new InvalidOperationException("No se pudo determinar la división del alumno.");

		var curso = await _unitOfWork.Cursos.BuscarPorIDAsync(division.CursoID)
			?? throw new InvalidOperationException("No se pudo determinar el curso del alumno.");

		var inscripcionesDelCiclo = await _unitOfWork.Cursantes.BuscarInscripcionesDelCicloAsync(alumnoID, cicloLectivo);
		var calificacionesDelCiclo = inscripcionesDelCiclo
			.SelectMany(x => x.Calificaciones)
			.ToList();

		return new ContextoCursante(alumno, cursante, division, curso, calificacionesDelCiclo.AsReadOnly());
	}

	/// <summary>
	/// Materias de la currícula vigente del curso: la que no tiene fecha de finalización.
	/// </summary>
	/// <remarks>
	/// Devuelve vacío si el curso no tiene currícula vigente, en lugar de fallar: un documento
	/// de materias sin currícula es un documento con la tabla vacía, que es informativo, y no un
	/// error de operación.
	/// </remarks>
	public async Task<IReadOnlyCollection<Materia>> MateriasVigentesDelCursoAsync(Guid cursoID)
	{
		var curriculas = await _unitOfWork.Curriculas.CurriculasSegunCursoAsync(cursoID);
		var vigente = curriculas.FirstOrDefault(x => x.Periodo.FechaFin is null);

		if (vigente is null)
		{
			return Array.Empty<Materia>();
		}

		return await _unitOfWork.Materias.BuscarMateriasSegunCurriculaAsync(vigente.Id);
	}

	/// <summary>Curso y división en el formato en que se leen en un documento: "3° Año B".</summary>
	public static string DescribirTrayectoria(Curso curso, Division division) =>
		$"{Describir(curso.Grado)} {division.Descripcion}";

	#region Lectura de calificaciones
	/// <summary>
	/// Las calificaciones de una materia, que es lo que esperan las reglas del dominio.
	/// </summary>
	/// <remarks>
	/// Filtrar por materia es trabajo de esta capa: <see cref="RendimientoAcademico"/> recibe el
	/// conjunto ya acotado porque agrupar requiere saber de dónde salieron las calificaciones, y
	/// eso lo sabe quien fue al repositorio.
	/// </remarks>
	private static List<Calificacion> DeLaMateria(IEnumerable<Calificacion> calificacionesDelCursante, Guid materiaID) =>
		calificacionesDelCursante.Where(x => x.MateriaID.Equals(materiaID)).ToList();

	/// <summary>
	/// Una materia está aprobada cuando su nota de cierre alcanza el umbral.
	/// </summary>
	/// <remarks>
	/// <strong>Acá no se decide nada.</strong> La regla es de <see cref="RendimientoAcademico"/>,
	/// en el dominio. Hasta que el modelo de instancias distinguió los períodos, este método
	/// decidía por su cuenta —"alguna calificación aprobada aprueba la materia"— y contradecía al
	/// perfil del alumno, que promediaba: con un 4 y un 7 el boletín imprimía "Aprobada" y el
	/// perfil "Desaprobada" sobre los mismos datos. Eran dos reglas, no dos vistas.
	/// </remarks>
	public static bool EstaAprobada(IEnumerable<Calificacion> calificacionesDelCursante, Guid materiaID) =>
		RendimientoAcademico.EstaAprobada(DeLaMateria(calificacionesDelCursante, materiaID));

	/// <summary>
	/// Condición de la materia. Con <paramref name="conNota"/> informa además la nota de cierre,
	/// que es la que explica la condición.
	/// </summary>
	/// <remarks>
	/// El parámetro existe porque las constancias y el boletín tienen lugar para la nota entre
	/// paréntesis y los reportes no: con diez columnas en una hoja, "Aprobada (8)" parte la
	/// celda en dos líneas. Lo que NO cambia entre los dos es cómo se decide la condición, y por
	/// eso se decide en el dominio una sola vez.
	/// </remarks>
	public static string DescribirCondicion(IEnumerable<Calificacion> calificacionesDelCursante, Guid materiaID, bool conNota)
	{
		var calificaciones = DeLaMateria(calificacionesDelCursante, materiaID);

		if (!calificaciones.Any())
		{
			return SIN_CALIFICAR;
		}

		var cierre = RendimientoAcademico.NotaDeCierre(calificaciones);
		if (cierre is null)
		{
			// Hay registros, pero son todos inasistencias al examen.
			return NO_RINDIO;
		}

		var condicion = RendimientoAcademico.EstaAprobada(calificaciones) ? "Aprobada" : "Desaprobada";

		return conNota ? $"{ condicion } ({ Formatear(cierre) })" : condicion;
	}

	/// <summary>
	/// La nota que quedó de un período, para imprimirla en una celda.
	/// </summary>
	/// <remarks>
	/// Devuelve la nota VIGENTE del período y no cada intento por separado: si hubo recuperatorio,
	/// la nota del período es la del recuperatorio, y el boletín informa el resultado del período
	/// —no su historia—. El detalle intento por intento está en la pantalla de calificaciones.
	///
	/// Distingue las dos formas de no tener nota, porque no significan lo mismo para quien lee el
	/// boletín: <see cref="SIN_DATO"/> es que el período no se tomó todavía, y
	/// <see cref="NO_RINDIO"/> es que se tomó y el alumno no se presentó.
	/// </remarks>
	public static string DescribirNota(IEnumerable<Calificacion> calificacionesDelCursante, Guid materiaID, Periodo periodo)
	{
		var calificaciones = DeLaMateria(calificacionesDelCursante, materiaID);
		var nota = RendimientoAcademico.NotaDelPeriodo(calificaciones, periodo);

		if (nota is not null)
		{
			return Formatear(nota);
		}

		return calificaciones.Any(x => x.Periodo.Equals(periodo)) ? NO_RINDIO : SIN_DATO;
	}

	/// <summary>
	/// La nota de recuperación, de los dos períodos en una sola celda.
	/// </summary>
	/// <remarks>
	/// Van juntas porque a la segunda recuperación se llega sólo después de no aprobar la primera,
	/// así que las dos nunca interesan a la vez: la que vale es la última que se rindió. Dos
	/// columnas que en casi todos los boletines estarían vacías no pagan el ancho de hoja.
	/// </remarks>
	public static string DescribirRecuperacion(IEnumerable<Calificacion> calificacionesDelCursante, Guid materiaID)
	{
		var calificaciones = DeLaMateria(calificacionesDelCursante, materiaID);
		var nota = RendimientoAcademico.NotaDeRecuperacion(calificaciones);

		if (nota is not null)
		{
			return Formatear(nota);
		}

		return calificaciones.Any(x => x.Periodo is Periodo.PrimeraRecuperacion or Periodo.SegundaRecuperacion)
			? NO_RINDIO
			: SIN_DATO;
	}

	/// <summary>Nota sin decimales cuando es entera: "8" en lugar de "8,00".</summary>
	private static string Formatear(decimal? nota) =>
		nota.HasValue ? nota.Value.ToString("0.##") : SIN_DATO;
	#endregion

	/// <summary>Texto de un <c>enum</c> según su <see cref="DescriptionAttribute"/>.</summary>
	/// <remarks>
	/// <see cref="Grado"/> ya declara su texto legible —"1° Año"— con ese atributo. Leerlo evita
	/// una segunda tabla de equivalencias que podría quedar desalineada al agregar un grado.
	/// </remarks>
	public static string Describir(Enum valor)
	{
		var miembro = valor.GetType().GetField(valor.ToString());
		var descripcion = miembro?.GetCustomAttribute<DescriptionAttribute>();

		return descripcion?.Description ?? valor.ToString();
	}
}
