namespace Core.ServicioAsistencias.DTOs.Responses;

/// <summary>
/// Una planilla del historial: la fecha, su estado y los cuatro contadores del día, sin los registros.
/// </summary>
/// <remarks>
/// Es deliberadamente más chico que <see cref="PlanillaAsistenciaResponse"/>. El historial lista
/// decenas de planillas y de cada una sólo necesita responder "qué día fue, está cerrada y cómo salió";
/// arrastrar los registros de todas las planillas del período para mostrar cuatro números sería traer
/// el ciclo entero a la pantalla. Para ver el detalle de un día se abre esa fecha, que es lo que ya
/// hace <c>ConsultarPlanillaAsync</c>.
/// </remarks>
public record PlanillaResumenResponse(
	Guid PlanillaID,
	DateTime Fecha,
	bool Cerrada,
	int Registros,
	int Presentes,
	int Ausencias,
	int Inasistencias,
	int Tardanzas);
