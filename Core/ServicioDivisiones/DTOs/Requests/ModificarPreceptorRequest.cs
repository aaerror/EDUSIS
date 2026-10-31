namespace Core.ServicioDivisiones.DTOs.Requests;

public record ModificarPreceptorRequest(Guid CursoID, Guid DivisionID, Guid DocenteID);
