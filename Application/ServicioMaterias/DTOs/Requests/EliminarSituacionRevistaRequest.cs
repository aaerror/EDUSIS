namespace Core.ServicioMaterias.DTOs.Requests;

public record EliminarSituacionRevistaRequest(Guid CursoID, Guid MateriaID, Guid DocenteID);