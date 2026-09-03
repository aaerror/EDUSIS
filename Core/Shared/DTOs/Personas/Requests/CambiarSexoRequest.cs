namespace Core.Shared.DTOs.Personas.Requests;

public record CambiarSexoRequest(Guid PersonaID, string Apellido, string Nombre, string Sexo);