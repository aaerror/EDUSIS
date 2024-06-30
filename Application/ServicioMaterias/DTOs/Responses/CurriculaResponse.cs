namespace Core.ServicioMaterias.DTOs.Responses;

public record CurriculaResponse(Guid CurriculaID, Guid CursoID, DateTime FechaInicio, DateTime? FechaFin);