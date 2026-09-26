namespace Core.ServicioCatedras.DTOs.Responses;

public record SituacionRevistaResponse(Guid SituacionRevistaID, Guid CatedraID, Guid DocenteID, string Estado, string Cargo, DateTime FechaInicio, DateTime? FechaFin, Guid? ReemplazaA, bool EnFunciones);
