namespace Core.ServicioDivisiones.DTOs.Responses;

/// <summary>
/// Qué se destruye si se elimina la división: la cuenta exacta de inscripciones, calificaciones,
/// jornadas de asistencia y cátedras que el borrado se lleva puestas. Lo consume el diálogo de
/// confirmación.
/// </summary>
/// <remarks>
/// <para>
/// Existe porque la división pasó de <strong>prohibir</strong> a <strong>cascadear</strong>
/// (2026-11-06). Antes el caso de uso rechazaba el borrado ante una sola inscripción histórica, así
/// que no había nada que informar; ahora el borrado destruye la trayectoria de toda la matrícula que
/// pasó por esa división, y una confirmación que no dice qué se pierde no es una confirmación.
/// </para>
/// <para>
/// Los números salen de las <strong>mismas tres consultas</strong> que después ejecutan el borrado
/// (<c>CatedrasSegunDivisionAsync</c>, <c>BuscarPorDivisionAsync</c> y
/// <c>BuscarInscripcionesDeDivisionAsync</c>), no de consultas paralelas: si fueran distintas, el
/// diálogo podría informar una cifra y el borrado hacer otra. Es el mismo criterio de
/// <c>ImpactoEliminacionAlumnoResponse</c>.
/// </para>
/// <para>
/// <c>Designaciones</c> y <c>RegistrosAsistencia</c> se informan aunque el caso de uso no las borre
/// una por una: caen por las cascadas ya declaradas en la base
/// (<c>FK_SITUACION_REVISTA_CATEDRA</c>, <c>FK_HORARIO_CATEDRA</c> y
/// <c>FK_REGISTRO_ASISTENCIA_PLANILLA_ASISTENCIA</c>). Que el borrado sea implícito no las hace
/// menos destruidas, y el usuario las tiene que ver.
/// </para>
/// </remarks>
public record ImpactoEliminacionDivisionResponse(
	int Inscripciones,
	int Calificaciones,
	int Planillas,
	int RegistrosAsistencia,
	int Catedras,
	int Designaciones,
	IReadOnlyCollection<string> CiclosLectivos)
{
	/// <summary>
	/// La división tiene algo registrado. En <c>false</c> el borrado no destruye nada más que la
	/// división en sí, y el diálogo puede decirlo en una línea en lugar de enumerar ceros.
	/// </summary>
	public bool TieneHistorial =>
		Inscripciones > 0 || Calificaciones > 0 || Planillas > 0 || Catedras > 0;
}
