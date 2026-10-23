namespace Core.ServicioDocumentos.DTOs.Requests;

/// <summary>
/// Parámetros de emisión de un documento de alumno: a quién, de qué ciclo lectivo y para qué.
/// </summary>
/// <remarks>
/// Uno solo para los cinco documentos que no necesitan más que esto. <c>Finalidad</c> es el
/// motivo del trámite y es opcional: se imprime únicamente si viene.
/// </remarks>
public record EmitirDocumentoAlumnoRequest(Guid AlumnoID, string Periodo, string? Finalidad);
