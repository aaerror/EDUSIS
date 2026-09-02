namespace Core.ServicioAlumnos.DTOs.Requests;

public record ContactoRequest(Guid PersonaID, string Email, string Telefono);
