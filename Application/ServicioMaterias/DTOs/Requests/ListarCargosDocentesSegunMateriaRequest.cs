namespace Core.ServicioMaterias.DTOs.Requests;

public record ListarCargosDocentesSegunMateriaRequest(Guid CursoID, Guid MateriaID);