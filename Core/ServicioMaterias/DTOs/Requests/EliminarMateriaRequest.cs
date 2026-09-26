namespace Core.ServicioMaterias.DTOs.Requests;

public record EliminarMateriaRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID);
