namespace Core.ServicioCursantes.DTOs.Requests;

public record CrearCalificationRequest(Guid AlumnoID, string Periodo, Guid MateriaID, DateTime Fecha, string Instancia, double Nota, string? Observacion);
