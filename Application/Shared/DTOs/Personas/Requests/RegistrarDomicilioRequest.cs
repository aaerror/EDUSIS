namespace Core.Shared.DTOs.Personas.Requests;

public record RegistrarDomicilioRequest(string Calle, string Altura, string Vivienda, string Observacion, string Localidad, string Provincia, string Pais);