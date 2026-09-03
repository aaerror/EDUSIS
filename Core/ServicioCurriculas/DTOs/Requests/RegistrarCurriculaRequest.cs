namespace Core.ServicioCurriculas.DTOs.Requests;

public record RegistrarCurriculaRequest(Guid CursoID, DateTime FechaInicio, DateTime? FechaFin);