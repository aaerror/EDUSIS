namespace Core.ServicioMaterias.DTOs.Requests;

public record ModificarMateriaRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID, string Descripcion, int HorasCatedra);
