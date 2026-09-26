namespace Core.ServicioCatedras.DTOs.Requests;

public record RescindirCargoDocenteRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID, Guid SituacionRevistaID);