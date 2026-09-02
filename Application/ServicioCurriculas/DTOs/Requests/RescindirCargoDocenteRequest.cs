namespace Core.ServicioCurriculas.DTOs.Requests;

public record RescindirCargoDocenteRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID, Guid SituacionRevistaID);