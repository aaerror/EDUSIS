namespace Core.ServicioDivisiones.DTOs.Requests;

public record EliminarDivisionRequest(Guid CursoID, Guid DivisionID, string CicloLectivo);
