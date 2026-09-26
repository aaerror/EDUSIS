namespace Core.ServicioCatedras.DTOs.Responses;

public record SituacionRevistaResponse(Guid SituacionRevistaID, Guid DocenteID, string Docente, string Estado, string Cargo, DateTime FechaAlta, DateTime? FechaBaja, bool EnFunciones);