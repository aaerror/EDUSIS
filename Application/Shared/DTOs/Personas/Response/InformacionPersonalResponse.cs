namespace Core.Shared.DTOs.Personas.Responses;

public record InformacionPersonalResponse(string Apellido, string Nombre, string DNI, int Sexo, DateTime FechaNacimiento, string Nacionalidad);