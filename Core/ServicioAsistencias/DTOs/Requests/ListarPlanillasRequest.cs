namespace Core.ServicioAsistencias.DTOs.Requests;

/// <summary>
/// Las planillas de una división entre dos fechas, ambos extremos incluidos.
/// </summary>
/// <remarks>
/// El rango no es opcional a propósito: la pantalla de asistencia trabaja sobre un período —el último
/// mes, la última quincena— y no sobre el ciclo entero. Traer todo por defecto es lo que vuelve
/// inusable un historial que crece una fila por día de clase.
/// </remarks>
public record ListarPlanillasRequest(Guid DivisionID, DateTime Desde, DateTime Hasta);
