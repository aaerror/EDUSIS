namespace Core.ServicioMaterias.DTOs.Requests;

public record EstablecerDocenteDeAulaRequest(Guid CursoID, Guid MateriaID, Guid DocenteID);