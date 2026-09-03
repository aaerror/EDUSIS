namespace Core.Shared.DTOs.Personas.Requests;

public record RegistrarDatosPersonalesRequest(string Apellido, string Nombre, string Documento, string Sexo, DateTime FechaNacimiento, string Nacionalidad);