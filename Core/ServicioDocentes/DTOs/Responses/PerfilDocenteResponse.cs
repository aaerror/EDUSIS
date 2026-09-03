using Core.Shared.DTOs.Personas.Responses;

namespace Core.ServicioDocentes.DTOs.Responses;

public record PerfilDocenteResponse(Guid DocenteID, DatosPersonalesResponse InformacionPersonalDTO, DomicilioResponse DomicilioDTO, ContactoResponse ContactoDTO);
