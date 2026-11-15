using Domain.Asistencias;

namespace Core.ServicioAsistencias.DTOs.Requests;

/// <summary>
/// Registros de un tipo de asistencia de un cursante, opcionalmente dentro de un período (RF-16).
/// Los extremos se incluyen; un extremo nulo deja el período abierto de ese lado.
/// </summary>
public record ContarFaltasRequest(Guid CursanteID, TipoAsistencia Tipo, DateTime? Desde = null, DateTime? Hasta = null);
