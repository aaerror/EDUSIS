namespace Core.ServicioCurriculas.DTOs.Requests;

public record RelevarDocenteDeAulaRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID);