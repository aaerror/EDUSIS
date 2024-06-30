using Core.Shared.DTOs.Personas.Responses;

namespace Core.ServicioDocentes.DTOs.Responses;

public record PerfilPersonalDeDocenteResponse(Guid DocenteID, InformacionPersonalResponse InformacionPersonalDTO, DomicilioResponse DomicilioDTO, ContactoResponse ContactoDTO);
