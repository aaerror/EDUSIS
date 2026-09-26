namespace Core.ServicioCursantes.DTOs.Responses;

public record CalificacionResponse(Guid CalificacionID, Guid MateriaID, string Materia, DateTime Fecha, string Instancia, bool Rindio, double? Nota, bool Aprobado, string? Observacion);
