using Domain.Cursos;

namespace Core.ServicioCursos.DTOs.Responses;

public record CursoResponse(Guid CursoID, Grado Grado, NivelEducativo NivelEducativo, int Divisiones, int Alumnos);