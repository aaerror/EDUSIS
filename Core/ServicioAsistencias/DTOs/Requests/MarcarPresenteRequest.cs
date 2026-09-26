namespace Core.ServicioAsistencias.DTOs.Requests;

public record MarcarPresenteRequest(Guid DivisionID, DateTime Fecha, Guid CursanteID);
