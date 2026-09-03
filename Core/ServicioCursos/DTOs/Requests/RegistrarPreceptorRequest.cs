namespace Core.ServicioCursos.DTOs.Requests;

public record RegistrarPreceptorRequest(Guid CursoID, Guid DivisionID, Guid DocenteID);