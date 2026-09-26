namespace Core.ServicioCatedras.DTOs.Requests;

public record ListarCargosDocenteSegunMateriaRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID);