namespace Core.Shared.DTOs.Personas.Response;

public record PersonaConDetallesResponse(Guid PersonaID, string Apellido, string Nombre, string Documento, string Sexo, string FechaNacimiento, string Nacionalidad, string Email, string Telefono, string Calle, string Altura, string Vivienda, string Observacion, string Localidad, string Provincia, string Pais);