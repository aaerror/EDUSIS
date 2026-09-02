namespace Core.ServicioCurriculas.DTOs.Requests;

public record RegistrarDocenteEnMateriaRequest(Guid CursoID, Guid CurriculaID, Guid MateriaID, Guid DocenteID, string Cargo, DateTime FechaAlta, DateTime? FechaBaja, bool EnFunciones);