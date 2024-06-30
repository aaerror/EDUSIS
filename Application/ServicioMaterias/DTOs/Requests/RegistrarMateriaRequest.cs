namespace Core.ServicioMaterias.DTOs.Requests;

public record RegistrarMateriaRequest(Guid CursoID, string Descripcion, int HorasCatedra);