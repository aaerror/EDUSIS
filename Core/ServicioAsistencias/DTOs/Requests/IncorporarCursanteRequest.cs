namespace Core.ServicioAsistencias.DTOs.Requests;

public record IncorporarCursanteRequest(Guid DivisionID, DateTime Fecha, Guid CursanteID);
