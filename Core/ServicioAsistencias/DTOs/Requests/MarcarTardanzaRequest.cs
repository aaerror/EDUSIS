namespace Core.ServicioAsistencias.DTOs.Requests;

public record MarcarTardanzaRequest(Guid DivisionID, DateTime Fecha, Guid CursanteID, TimeSpan Minutos, string? Observacion = null);
