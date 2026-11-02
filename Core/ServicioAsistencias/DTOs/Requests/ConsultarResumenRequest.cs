namespace Core.ServicioAsistencias.DTOs.Requests;

/// <summary>
/// El acumulado de asistencia de una división entre dos fechas, ambos extremos incluidos.
/// </summary>
public record ConsultarResumenRequest(Guid DivisionID, DateTime Desde, DateTime Hasta);
