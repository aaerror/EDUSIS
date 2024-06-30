namespace Core.ServicioCursos.DTOs.Responses;

public record CursoResponse(Guid CursoID, int Grado, string GradoDescripcion, int NivelEducativo, string NivelEducativoDescripcion, int Divisiones, int Alumnos);