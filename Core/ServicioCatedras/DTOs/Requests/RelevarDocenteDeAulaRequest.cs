namespace Core.ServicioCatedras.DTOs.Requests;

public record RelevarDocenteDeAulaRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID);