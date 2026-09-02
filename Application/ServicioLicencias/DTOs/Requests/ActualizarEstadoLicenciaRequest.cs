namespace Core.ServicioLicencias.DTOs.Requests;

public record ActualizarEstadoLicenciaRequest(Guid LicenciaID, Guid DocenteID, string? Observacion);