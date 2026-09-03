namespace Core.ServicioCurriculas.DTOs.Responses;

public record MateriaResponse(Guid CursoID, Guid CurriculaID, Guid MateriaID, string Descripcion, int HorasCatedra, int CargosOcupados, SituacionRevistaResponse? SituacionRevistaResponse);