namespace Core.Shared.DTOs.Personas.Requests;

public record RegistrarInformacionPersonalRequest(string Apellido, string Nombre, string Documento, int Sexo, DateTime FechaNacimiento, string Nacionalidad);