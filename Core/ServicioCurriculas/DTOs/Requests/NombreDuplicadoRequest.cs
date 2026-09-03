namespace Core.ServicioCurriculas.DTOs.Requests;

public record NombreDuplicadoRequest(Guid CursoID, Guid? MateriaID, string Descripcion);