namespace Core.ServicioReportes.DTOs.Requests;

/// <summary>
/// Alcance de un reporte institucional: un ciclo lectivo y, opcionalmente, una sola división.
/// </summary>
/// <remarks>
/// <c>DivisionID</c> en nulo significa "toda la institución", que es el caso habitual de un
/// reporte. El filtro existe porque un listado de la matrícula completa es inmanejable cuando lo
/// que se quiere revisar es un curso.
/// </remarks>
public record ReportePorCicloRequest(string Periodo, Guid? DivisionID);
