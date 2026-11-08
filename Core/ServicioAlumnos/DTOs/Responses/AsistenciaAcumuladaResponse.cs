namespace Core.ServicioAlumnos.DTOs.Responses;

/// <summary>
/// La asistencia acumulada de un alumno en un ciclo lectivo: una fila, no una por jornada.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Faltas"/> es <c>Ausencias + Inasistencias</c>, el mismo criterio de
/// <c>ResumenAsistenciaResponse</c>: las dos son inasistencia a clase, y se siguen informando por
/// separado porque una está justificada y la otra no.
/// </para>
/// <para>
/// <see cref="Jornadas"/> cuenta las marcas del alumno, no las planillas de la división: un alumno
/// que se incorporó en mayo no tiene registro en las planillas de marzo, y contarlas como jornadas
/// suyas le inventaría faltas que nadie le tomó.
/// </para>
/// <para>
/// No hay umbral de "alumno en riesgo": cuántas faltas se toleran antes de perder la regularidad
/// es una regla de la institución, no de esta consulta.
/// </para>
/// </remarks>
public record AsistenciaAcumuladaResponse(
	int Jornadas,
	int Presentes,
	int Ausencias,
	int Inasistencias,
	int Tardanzas,
	int Faltas,
	decimal Porcentaje)
{
	/// <summary>
	/// Se le tomó asistencia alguna vez en el ciclo. En <c>false</c> el porcentaje es cero porque no
	/// hay jornadas, y la pantalla tiene que decir "sin registro" y no "0% de asistencia".
	/// </summary>
	public bool TieneRegistro => Jornadas > 0;

	/// <summary>Un acumulado vacío, para el alumno que todavía no tiene ninguna marca.</summary>
	public static AsistenciaAcumuladaResponse SinRegistro() =>
		new(Jornadas: 0, Presentes: 0, Ausencias: 0, Inasistencias: 0, Tardanzas: 0, Faltas: 0, Porcentaje: decimal.Zero);
}
