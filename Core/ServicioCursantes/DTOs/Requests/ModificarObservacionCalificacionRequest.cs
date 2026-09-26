namespace Core.ServicioCursantes.DTOs.Requests;

public record ModificarObservacionCalificacionRequest(Guid AlumnoID, string Periodo, Guid CalificacionID, string? Observacion);
