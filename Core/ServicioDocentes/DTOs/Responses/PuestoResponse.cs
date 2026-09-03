namespace Core.ServicioDocentes.DTOs.Responses;

public record PuestoResponse(Guid PuestoID, string Estado, string Posicion, bool EsEventual, DateTime FechaInicio, DateTime? FechaFin, bool Activo);