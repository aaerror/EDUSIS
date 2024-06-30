namespace Core.ServicioDocentes.DTOs.Responses;

public record LegajoDocenteResponse(Guid DocenteID, string NombreCompleto, string Legajo, DateTime FechaAlta, DateTime? FechaBaja, string CUIL, bool EstaActivo, IReadOnlyCollection<PuestoResponse> Puestos);