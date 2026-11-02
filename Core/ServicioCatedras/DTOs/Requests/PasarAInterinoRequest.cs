namespace Core.ServicioCatedras.DTOs.Requests;

/// <summary>
/// Le pone término a una designación indeterminada: el cargo pasa a interino y vence en
/// <paramref name="FechaFin"/>, que el dominio exige posterior a hoy.
/// </summary>
public record PasarAInterinoRequest(Guid CatedraID, Guid SituacionRevistaID, DateTime FechaFin);
