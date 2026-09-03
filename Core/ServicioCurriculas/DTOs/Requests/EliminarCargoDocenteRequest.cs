namespace Core.ServicioCurriculas.DTOs.Requests;

public record EliminarCargoDocenteRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID, Guid SituacionRevistaID);