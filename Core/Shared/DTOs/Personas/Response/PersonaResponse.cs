namespace Core.Shared.DTOs.Personas.Response;

public record PersonaResponse(Guid PersonaID, string Apellido, string Nombre, string Documento, string Sexo, string FechaNacimiento, string Nacionalidad);