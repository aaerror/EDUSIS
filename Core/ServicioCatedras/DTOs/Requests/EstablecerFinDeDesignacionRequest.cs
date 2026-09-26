namespace Core.ServicioCatedras.DTOs.Requests;

public record EstablecerFinDeDesignacionRequest(Guid CatedraID, Guid SituacionRevistaID, DateTime FechaFin);
