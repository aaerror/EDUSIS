namespace Core.ServicioCurriculas.DTOs.Requests;

public record ModificarMateriaRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID, string Descripcion, int HorasCatedra);