namespace Core.ServicioDocentes.DTOs.Responses;

public record LegajoDocenteResponse(Guid DocenteID, string NombreCompleto, string DNI, string CUIL, string Legajo, DateTime FechaInicio, DateTime? FechaFin, bool Activo);