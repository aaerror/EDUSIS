namespace Core.ServicioDocentes.DTOs.Responses;

public record DocenteInfoResponse(Guid DocenteID, string NombreCompleto, LegajoDocenteResponse Institucional);