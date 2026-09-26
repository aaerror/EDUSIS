namespace Core.ServicioMaterias.DTOs.Requests;

public record NombreDuplicadoRequest(Guid CurriculaID, Guid? MateriaID, string Descripcion);
