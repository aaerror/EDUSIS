namespace Core.ServicioMaterias.DTOs.Requests;

public record NombreDuplicadoRequest(Guid CursoID, Guid? MateriaID, string Descripcion);