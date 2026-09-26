namespace Core.ServicioCursantes.DTOs.Requests;

public record EliminarCalificacionRequest(Guid AlumnoID, string Periodo, Guid CalificacionID);
