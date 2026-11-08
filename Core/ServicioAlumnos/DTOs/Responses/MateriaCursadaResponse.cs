namespace Core.ServicioAlumnos.DTOs.Responses;

/// <summary>
/// Una materia del ciclo con el rendimiento del alumno en ella: cuántas veces rindió, cuántas
/// faltó al examen, su promedio en la materia y si está aprobada.
/// </summary>
/// <remarks>
/// <para>
/// Agrupa por materia y no por calificación porque una materia tiene varias instancias —primera
/// nota, recuperatorio, final—, y la pregunta que trae a alguien al perfil es "cómo le va en
/// Matemática", no "qué saco en el recuperatorio de agosto". El detalle instancia por instancia es
/// el boletín, que ya existe como documento.
/// </para>
/// <para>
/// <see cref="Promedio"/> es nulo cuando el alumno no rindió ninguna instancia de la materia y sólo
/// tiene ausencias a examen: no hay nota de la que promediar, y un cero diría que rindió y le fue
/// mal.
/// </para>
/// </remarks>
public record MateriaCursadaResponse(
	Guid MateriaID,
	string Materia,
	int Rendidas,
	int AusenciasAExamen,
	decimal? Promedio,
	bool Aprobada);
