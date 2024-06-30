namespace Core.ServicioDocentes.DTOs.Responses;

public record PuestoResponse(int Posicion, string PosicionDescripcion, DateTime FechaInicio, DateTime? FechaFin);