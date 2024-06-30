namespace Core.Shared.DTOs.Personas.Requests;

public record RegistrarDomicilioRequest(string Calle, string Altura, int Vivienda, string Observacion, string Localidad, string Provincia, string Pais);