namespace Core.Shared.DTOs.Personas.Requests;

public record CambiarDomicilioRequest(Guid PersonaID, string Calle, string Altura, string Vivienda, string Observacion, string Localidad, string Provincia, string Pais);