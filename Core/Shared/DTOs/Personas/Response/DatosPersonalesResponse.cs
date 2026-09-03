namespace Core.Shared.DTOs.Personas.Responses;

public record DatosPersonalesResponse(string Apellido, string Nombre, string DNI, string Sexo, DateTime FechaNacimiento, string Nacionalidad);