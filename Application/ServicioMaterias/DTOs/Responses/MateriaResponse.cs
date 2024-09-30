namespace Core.ServicioMaterias.DTOs.Responses;

public record MateriaResponse(Guid CursoID, Guid MateriaID, string Descripcion, int HorasCatedra, int CargosOcupados, SituacionRevistaResponse? SituacionRevistaResponse);