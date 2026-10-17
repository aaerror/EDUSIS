using Domain.Cursos;
using Domain.Shared;

namespace EDUSIS.Seed;

/// <summary>
/// Catálogo fijo de la escuela demo: qué cursos existen y qué espacios curriculares tiene cada
/// año. Está escrito a mano y no generado con Bogus a propósito — un plan de estudios inventado
/// al azar produce datos que no se pueden leer para verificar una pantalla ("¿está bien que
/// 3° B tenga 'Lorem Ipsum' con 7 horas?"), mientras que un plan plausible sí.
/// </summary>
internal static class EscuelaDemo
{
	/// <summary>Divisiones por curso. Son letras porque <c>Division</c> exige una sola letra.</summary>
	internal static readonly string[] Divisiones = { "A", "B" };

	internal const NivelEducativo Nivel = NivelEducativo.Secundaria;

	/// <summary>Los seis años del secundario, en orden.</summary>
	internal static readonly Grado[] Cursos =
	{
		Grado.Primero,
		Grado.Segundo,
		Grado.Tercero,
		Grado.Cuarto,
		Grado.Quinto,
		Grado.Sexto
	};

	/// <summary>
	/// Plan de estudios por año. La carga horaria suma 32 horas cátedra en cada año, que entran
	/// en los 45 slots disponibles del turno mañana (07:00–13:00, hora cátedra de 40 minutos,
	/// cinco días hábiles) sin que dos cátedras de la misma división se pisen.
	/// </summary>
	internal static IReadOnlyList<EspacioCurricular> PlanDeEstudios(Grado grado) =>
		grado switch
		{
			Grado.Primero => new EspacioCurricular[]
			{
				new("Lengua y Literatura", 5),
				new("Matemática", 5),
				new("Ciencias Naturales", 4),
				new("Ciencias Sociales", 4),
				new("Inglés", 3),
				new("Educación Física", 3),
				new("Educación Tecnológica", 4),
				new("Educación Artística", 4)
			},
			Grado.Segundo => new EspacioCurricular[]
			{
				new("Lengua y Literatura", 5),
				new("Matemática", 5),
				new("Biología", 4),
				new("Historia", 4),
				new("Geografía", 3),
				new("Inglés", 3),
				new("Educación Física", 3),
				new("Educación Tecnológica", 5)
			},
			Grado.Tercero => new EspacioCurricular[]
			{
				new("Lengua y Literatura", 5),
				new("Matemática", 5),
				new("Física", 4),
				new("Química", 4),
				new("Historia", 4),
				new("Geografía", 3),
				new("Inglés", 3),
				new("Educación Física", 4)
			},
			Grado.Cuarto => new EspacioCurricular[]
			{
				new("Literatura", 4),
				new("Matemática", 5),
				new("Física", 4),
				new("Química", 4),
				new("Historia", 4),
				new("Economía", 4),
				new("Inglés", 4),
				new("Educación Física", 3)
			},
			Grado.Quinto => new EspacioCurricular[]
			{
				new("Literatura", 4),
				new("Análisis Matemático", 5),
				new("Física", 4),
				new("Historia Argentina", 4),
				new("Geografía Argentina", 4),
				new("Derecho", 4),
				new("Inglés", 4),
				new("Educación Física", 3)
			},
			Grado.Sexto => new EspacioCurricular[]
			{
				new("Literatura Argentina", 4),
				new("Matemática Aplicada", 5),
				new("Filosofía", 4),
				new("Economía Política", 4),
				new("Proyecto de Investigación", 5),
				new("Ciudadanía y Trabajo", 3),
				new("Inglés Técnico", 4),
				new("Educación Física", 3)
			},
			_ => Array.Empty<EspacioCurricular>()
		};

	/// <summary>Un espacio curricular del plan: cómo se llama y cuántas horas cátedra tiene.</summary>
	internal readonly record struct EspacioCurricular(string Descripcion, int HorasCatedra);
}
