namespace Core.ServicioCurriculas.DTOs.Responses;

public record CurriculaResponse(Guid CursoID, Guid CurriculaID, DateTime FechaInicio, DateTime? FechaFin, IReadOnlyCollection<MateriaResponse> Materias);