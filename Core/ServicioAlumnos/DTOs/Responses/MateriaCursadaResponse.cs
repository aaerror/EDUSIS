namespace Core.ServicioAlumnos.DTOs.Responses;

/// <summary>
/// Una materia del ciclo con el rendimiento del alumno en ella: la nota que quedó de cada período,
/// la nota con la que cierra, cuántas veces faltó al examen y si está aprobada.
/// </summary>
/// <remarks>
/// <para>
/// Agrupa por materia y no por calificación porque una materia tiene hasta nueve instancias en un
/// ciclo —dos cuatrimestres y dos recuperaciones, cada uno con su recuperatorio, más la nota
/// final—, y la pregunta que trae a alguien al perfil es "cómo le va en Matemática", no "qué sacó
/// en el recuperatorio de agosto". El detalle instancia por instancia es el boletín, que ya existe
/// como documento.
/// </para>
/// <para>
/// Las notas de cada período son la nota VIGENTE de ese período, no cada intento: si hubo
/// recuperatorio, es la del recuperatorio, porque reemplaza a la original. Son nulas cuando el
/// período todavía no se tomó o cuando el alumno no se presentó, que para esta tabla es lo mismo:
/// no hay nota. El contador de <see cref="AusenciasAExamen"/> es el que distingue los dos casos.
/// </para>
/// <para>
/// <see cref="Recuperacion"/> es una sola columna para los dos períodos de recuperación porque a
/// la segunda se llega sólo después de no aprobar la primera: la que importa es la última rendida.
/// </para>
/// <para>
/// <see cref="NotaDeCierre"/> es la que decide la condición, y es nula cuando no hay con qué
/// cerrar la materia: ni recuperación, ni nota final, ni un cuatrimestre rendido. Un cero diría
/// que rindió y le fue mal.
/// </para>
/// </remarks>
public record MateriaCursadaResponse(
	Guid MateriaID,
	string Materia,
	decimal? PrimerCuatrimestre,
	decimal? SegundoCuatrimestre,
	decimal? Final,
	decimal? Recuperacion,
	decimal? NotaDeCierre,
	int Rendidas,
	int AusenciasAExamen,
	bool Aprobada);
