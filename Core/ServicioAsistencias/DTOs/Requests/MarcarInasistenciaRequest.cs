namespace Core.ServicioAsistencias.DTOs.Requests;

public record MarcarInasistenciaRequest(Guid DivisionID, DateTime Fecha, Guid CursanteID, string? Observacion = null);
