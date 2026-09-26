namespace Core.ServicioAsistencias.DTOs.Requests;

public record CerrarPlanillaRequest(Guid DivisionID, DateTime Fecha);
