namespace Core.ServicioCurriculas.DTOs.Requests;

public record EstablecerDocenteDeAulaRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID, Guid SituacionRevistaID);