namespace Core.ServicioReportes.DTOs.Requests;

/// <summary>
/// Alcance del reporte de asistencias: además del ciclo lectivo y la división opcional, el rango
/// de fechas sobre el que se computan las planillas.
/// </summary>
public record ReporteAsistenciasRequest(string Periodo, DateTime Desde, DateTime Hasta, Guid? DivisionID);
