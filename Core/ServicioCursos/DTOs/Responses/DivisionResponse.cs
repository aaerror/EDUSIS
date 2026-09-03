namespace Core.ServicioCursos.DTOs.Responses;

public record DivisionResponse(Guid DivisionID, string Descripcion, Guid? DocenteID, string? Docente, int Alumnos);