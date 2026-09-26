namespace Core.ServicioCatedras.DTOs.Requests;

public record FinalizarDesignacionRequest(Guid CatedraID, Guid SituacionRevistaID);
