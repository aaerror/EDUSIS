namespace Core.Shared.DTOs.Personas.Responses;

public record PersonaResponse(Guid PersonaID, string Apellido, string Nombre, string Documento, string Sexo, string FechaNacimiento, string Nacionalidad);