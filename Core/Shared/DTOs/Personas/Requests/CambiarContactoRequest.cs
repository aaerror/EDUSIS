namespace Core.Shared.DTOs.Personas.Requests;

public record CambiarContactoRequest(Guid PersonaID, string Telefono, string Email);