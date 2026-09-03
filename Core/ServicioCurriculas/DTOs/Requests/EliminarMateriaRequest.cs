namespace Core.ServicioCurriculas.DTOs.Requests;

public record EliminarMateriaRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID);