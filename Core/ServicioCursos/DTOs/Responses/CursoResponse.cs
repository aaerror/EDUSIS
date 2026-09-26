using Domain.Cursos;
using Domain.Shared;

namespace Core.ServicioCursos.DTOs.Responses;

public record CursoResponse(Guid CursoID, Grado Grado, NivelEducativo NivelEducativo);