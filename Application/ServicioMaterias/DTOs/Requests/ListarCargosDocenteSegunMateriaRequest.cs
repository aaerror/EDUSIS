namespace Core.ServicioMaterias.DTOs.Requests;

public record ListarCargosDocenteSegunMateriaRequest(Guid CursoID, Guid MateriaID);