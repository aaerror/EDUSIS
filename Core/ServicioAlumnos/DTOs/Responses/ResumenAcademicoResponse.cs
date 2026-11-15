using Core.Shared.DTOs.Cursadas;

namespace Core.ServicioAlumnos.DTOs.Responses;

/// <summary>
/// La situación académica de un alumno: qué cursa, cómo le va y cuánto asiste. Es el modelo de
/// lectura que le faltaba a la pantalla de alumnos, que sólo sabía decir nombre, documento, fecha
/// de nacimiento, sexo y nacionalidad.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Todo lo que informa pertenece a un solo ciclo lectivo</strong>, el que nombra
/// <see cref="CicloLectivo"/>: el promedio, las materias y la asistencia son de ese período y de
/// ningún otro. Un promedio que mezclara cinco años de cursada no describiría a nadie, y uno sin
/// período declarado obligaría a la pantalla a rotularlo "Promedio" a secas, que es exactamente la
/// ambigüedad que hace inútil al número.
/// </para>
/// <para>
/// El ciclo elegido es el de la inscripción <strong>vigente</strong>. Si el alumno no está
/// cursando, es el de su inscripción más reciente, y la pantalla lo rotula igual —"Promedio
/// 2024"—, así que el número sigue siendo honesto sin cambiar de significado. Con un alumno sin
/// ninguna inscripción, <see cref="CicloLectivo"/> es nulo y no hay nada que informar.
/// </para>
/// <para>
/// <see cref="Promedio"/> es la media de las NOTAS DE CIERRE de las materias, una por materia, no
/// la media de todas las calificaciones cargadas. Promediar las instancias sueltas mezclaría la
/// nota de un cuatrimestre con la de su propio recuperatorio —que la reemplazó— y con las de los
/// períodos de recuperación, y le daría más peso a las materias con más exámenes tomados.
/// </para>
/// <para>
/// <see cref="Trayectoria"/> reutiliza <see cref="InscripcionHistoricaResponse"/>, el DTO de
/// <c>ServicioCursantes</c>, en lugar de declarar un registro propio idéntico: la cursada de un
/// alumno es la misma cosa vista desde el perfil o desde la división, y dos registros iguales con
/// nombres distintos divergen en cuanto a uno se le agregue un campo.
/// </para>
/// </remarks>
public record ResumenAcademicoResponse(
	Guid AlumnoID,
	string? CicloLectivo,
	InscripcionHistoricaResponse? Cursada,
	decimal? Promedio,
	int Rendidas,
	int AusenciasAExamen,
	int MateriasAprobadas,
	int MateriasDesaprobadas,
	AsistenciaAcumuladaResponse Asistencia,
	IReadOnlyCollection<MateriaCursadaResponse> Materias,
	IReadOnlyCollection<InscripcionHistoricaResponse> Trayectoria)
{
	/// <summary>
	/// Tiene una inscripción vigente. En <c>false</c> el alumno está registrado en la institución y
	/// no está cursando, que es un estado válido —egresado, baja, alta sin inscribir todavía— y no
	/// un error.
	/// </summary>
	public bool EstaCursando => Cursada is not null;

	/// <summary>
	/// Hay algo que decir del rendimiento en el ciclo. En <c>false</c> la pantalla no muestra un
	/// promedio vacío ni una tabla de materias sin filas: dice que todavía no se le cargó ninguna
	/// calificación.
	/// </summary>
	public bool TieneCalificaciones => Rendidas > 0 || AusenciasAExamen > 0;

	/// <summary>
	/// Nada que informar: el alumno no tiene ninguna inscripción, así que no hay ciclo del que
	/// hablar.
	/// </summary>
	public static ResumenAcademicoResponse SinTrayectoria(Guid alumnoID) =>
		new(AlumnoID: alumnoID,
			CicloLectivo: null,
			Cursada: null,
			Promedio: null,
			Rendidas: 0,
			AusenciasAExamen: 0,
			MateriasAprobadas: 0,
			MateriasDesaprobadas: 0,
			Asistencia: AsistenciaAcumuladaResponse.SinRegistro(),
			Materias: Array.Empty<MateriaCursadaResponse>(),
			Trayectoria: Array.Empty<InscripcionHistoricaResponse>());
}
