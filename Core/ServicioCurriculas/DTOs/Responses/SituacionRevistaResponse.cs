namespace Core.ServicioCurriculas.DTOs.Responses;

public record SituacionRevistaResponse(Guid SituacionRevistaID, Guid MateriaID, Guid DocenteID, string Docente, string Estado, string Cargo, DateTime FechaAlta, DateTime? FechaBaja, bool EnFunciones);