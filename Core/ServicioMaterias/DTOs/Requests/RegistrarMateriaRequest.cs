namespace Core.ServicioMaterias.DTOs.Requests;

public record RegistrarMateriaRequest(Guid CursoID, Guid CurriculaID, string Descripcion, int HorasCatedra);
