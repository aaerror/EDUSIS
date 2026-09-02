namespace Core.ServicioCursos.DTOs.Requests;

public record RegistrarCursanteRequest(Guid CursoID, Guid DivisionID, Guid AlumnoID, string Periodo);