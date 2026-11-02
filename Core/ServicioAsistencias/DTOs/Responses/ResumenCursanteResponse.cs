namespace Core.ServicioAsistencias.DTOs.Responses;

/// <summary>
/// El acumulado de un alumno dentro del período consultado.
/// </summary>
/// <remarks>
/// <para>
/// NO TRAE EL NOMBRE, y es la misma regla que <see cref="RegistroAsistenciaResponse"/>: la planilla
/// identifica a cada alumno por su <c>CursanteID</c> y los nombres viven en <c>IServicioCursante</c>.
/// Quien arma la pantalla une las dos fuentes, igual que lo hace la grilla del día.
/// </para>
/// <para>
/// <see cref="Jornadas"/> es la cantidad de planillas del período en las que este alumno tiene
/// registro, y no la cantidad de planillas del período: un alumno incorporado a mitad de mes tiene
/// menos jornadas que sus compañeros, y dividir sus presentes por el total de días de clase le
/// inventaría faltas que nadie le marcó.
/// </para>
/// </remarks>
public record ResumenCursanteResponse(
	Guid CursanteID,
	int Jornadas,
	int Presentes,
	int Ausencias,
	int Inasistencias,
	int Tardanzas,
	int Faltas,
	decimal PorcentajeAsistencia);
