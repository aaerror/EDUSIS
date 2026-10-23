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
internal sealed record ContextoCursante(Alumno Alumno, Cursante Cursante, Division Division, Curso Curso);

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
	/// Inscripción activa del alumno en el ciclo lectivo, con su división y curso.
	/// </summary>
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

		return new ContextoCursante(alumno, cursante, division, curso);
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
	/// Una materia está aprobada si alguna de sus calificaciones lo está, sin importar la
	/// instancia: aprobar en el recuperatorio es aprobar.
	/// </summary>
	public static bool EstaAprobada(Cursante cursante, Guid materiaID) =>
		cursante.Calificaciones.Any(x => x.MateriaID.Equals(materiaID) && x.EstaAprobado());

	/// <summary>
	/// Condición de la materia. Con <paramref name="conNota"/> informa además la mejor nota
	/// obtenida, que es la que explica la condición.
	/// </summary>
	/// <remarks>
	/// El parámetro existe porque las constancias y el boletín tienen lugar para la nota entre
	/// paréntesis y los reportes no: con diez columnas en una hoja, "Aprobada (8)" parte la
	/// celda en dos líneas. Lo que NO cambia entre los dos es cómo se decide la condición, y por
	/// eso se decide acá una sola vez.
	/// </remarks>
	public static string DescribirCondicion(Cursante cursante, Guid materiaID, bool conNota)
	{
		var calificaciones = cursante.Calificaciones.Where(x => x.MateriaID.Equals(materiaID)).ToList();

		if (!calificaciones.Any())
		{
			return SIN_CALIFICAR;
		}

		var mejor = calificaciones.Where(x => x.Rindio && x.Nota.HasValue).MaxBy(x => x.Nota);
		if (mejor is null)
		{
			// Hay registros, pero son todos inasistencias al examen.
			return NO_RINDIO;
		}

		var condicion = mejor.EstaAprobado() ? "Aprobada" : "Desaprobada";

		return conNota ? $"{ condicion } ({ Formatear(mejor.Nota) })" : condicion;
	}

	/// <summary>
	/// Nota de la materia en una instancia. Si hay más de una, vale la última por fecha: es la
	/// que refleja el estado actual de la cursada.
	/// </summary>
	public static string DescribirNota(Cursante cursante, Guid materiaID, Instancia instancia)
	{
		var ultima = cursante.Calificaciones
			.Where(x => x.MateriaID.Equals(materiaID) && x.Instancia.Equals(instancia))
			.OrderBy(x => x.Fecha)
			.LastOrDefault();

		if (ultima is null)
		{
			return SIN_DATO;
		}

		return ultima.Rindio ? Formatear(ultima.Nota) : NO_RINDIO;
	}

	/// <summary>Nota sin decimales cuando es entera: "8" en lugar de "8,00".</summary>
	private static string Formatear(double? nota) =>
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
