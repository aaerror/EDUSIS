namespace Core.ServicioAsistencias.DTOs.Responses;

/// <summary>
/// El acumulado de asistencia de una división en un período: el total de la división y una fila por
/// alumno, ordenada de más faltas a menos.
/// </summary>
/// <remarks>
/// <para>
/// Este es el read model que faltaba. Hasta acá lo único que se podía preguntar por alumno era
/// <c>ContarFaltasAsync(cursanteID, tipo)</c>, un contador de toda su historia sin fecha de corte, así
/// que una pantalla que quisiera "las faltas de este mes" tenía que traerse las planillas del período
/// y recorrerlas, es decir: armar la proyección en la capa de presentación. La proyección vive acá.
/// </para>
/// <para>
/// <see cref="Faltas"/> es <c>Ausencias + Inasistencias</c>, la misma cuenta que la pantalla ya hacía
/// al cerrar la planilla del día. Las dos se suman porque las dos son inasistencia a clase; se siguen
/// informando por separado porque una está justificada y la otra no, y esa diferencia es la que el
/// preceptor necesita para decidir.
/// </para>
/// <para>
/// No hay un umbral de "alumno en riesgo" ni un máximo de faltas permitidas: cuántas faltas se toleran
/// antes de perder la regularidad es una regla de la institución, no de esta consulta. Acá se cuenta y
/// se ordena; quien mire la pantalla pone el corte.
/// </para>
/// </remarks>
public record ResumenAsistenciaResponse(
	DateTime Desde,
	DateTime Hasta,
	int Planillas,
	int PlanillasAbiertas,
	int Marcas,
	int Presentes,
	int Ausencias,
	int Inasistencias,
	int Tardanzas,
	decimal PorcentajeAsistencia,
	IReadOnlyCollection<ResumenCursanteResponse> Cursantes);
