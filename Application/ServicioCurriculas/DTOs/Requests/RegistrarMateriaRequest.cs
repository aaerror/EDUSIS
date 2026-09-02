namespace Core.ServicioCurriculas.DTOs.Requests;

public record RegistrarMateriaRequest(Guid CursoID, Guid CurriculaID, string Descripcion, int HorasCatedra);