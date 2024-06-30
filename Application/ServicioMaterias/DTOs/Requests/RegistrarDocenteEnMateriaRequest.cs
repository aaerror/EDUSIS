namespace Core.ServicioMaterias.DTOs.Requests;

public record RegistrarDocenteEnMateriaRequest(Guid CursoID, Guid MateriaID, Guid DocenteID, int Cargo, DateTime FechaAlta, bool EnFunciones);