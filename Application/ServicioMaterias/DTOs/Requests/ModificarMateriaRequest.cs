namespace Core.ServicioMaterias.DTOs.Requests;

public record ModificarMateriaRequest(Guid CursoID, Guid MateriaID, string Descripcion, int HorasCatedra);