namespace Core.ServicioLicencias.DTOs.Responses;

public record LicenciaResponse(Guid LicenciaID, Guid DocenteID, string Articulo, string Estado, int Dias, DateTime FechaInicio, DateTime? FechaFin, string? Observacion);