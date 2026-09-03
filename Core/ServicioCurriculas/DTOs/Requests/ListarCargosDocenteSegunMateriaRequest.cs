namespace Core.ServicioCurriculas.DTOs.Requests;

public record ListarCargosDocenteSegunMateriaRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID);