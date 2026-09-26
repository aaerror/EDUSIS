namespace Core.ServicioAsistencias.DTOs.Requests;

public record MarcarAusenciaRequest(Guid DivisionID, DateTime Fecha, Guid CursanteID, string? Observacion = null);
