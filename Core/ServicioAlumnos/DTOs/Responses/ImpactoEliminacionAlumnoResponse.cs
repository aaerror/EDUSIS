namespace Core.ServicioAlumnos.DTOs.Responses;

/// <summary>
/// Qué se destruye si se elimina el alumno. Lo consume el diálogo de confirmación: la cuenta exacta
/// de inscripciones, calificaciones y registros de asistencia que el borrado se lleva puestos.
/// </summary>
/// <remarks>
/// <para>
/// Existe porque una confirmación que no dice qué se pierde no es una confirmación. La que había
/// afirmaba que "las inscripciones y calificaciones ya registradas quedan en el historial de la
/// división", que además de vago era <strong>falso</strong>: con <c>FK_CURSANTE_ALUMNO</c> en
/// <c>Restrict</c>, el borrado de un alumno con una sola inscripción no conservaba nada porque
/// fallaba contra la clave ajena.
/// </para>
/// <para>
/// Los números salen de las mismas dos consultas que después ejecutan el borrado
/// (<c>BuscarInscripcionesDelAlumnoAsync</c> y <c>BuscarPorCursantesAsync</c>), no de consultas
/// paralelas: si fueran distintas, el diálogo podría informar una cifra y el borrado hacer otra.
/// </para>
/// </remarks>
public record ImpactoEliminacionAlumnoResponse(
	int Inscripciones,
	int Calificaciones,
	int RegistrosAsistencia,
	int PlanillasQueQuedanVacias,
	IReadOnlyCollection<string> CiclosLectivos)
{
	/// <summary>
	/// El alumno tiene trayectoria registrada. En <c>false</c> el borrado no destruye nada más que
	/// el legajo, y el diálogo puede decirlo en una línea en lugar de enumerar ceros.
	/// </summary>
	public bool TieneHistorial =>
		Inscripciones > 0 || Calificaciones > 0 || RegistrosAsistencia > 0;
}
