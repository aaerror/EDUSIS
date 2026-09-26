namespace Core.ServicioCursantes.DTOs.Requests;

public record RegistrarInasistenciaRequest(Guid AlumnoID, string Periodo, Guid MateriaID, DateTime Fecha, string Instancia, string? Observacion);
