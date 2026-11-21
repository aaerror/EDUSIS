namespace Core.ServicioLicencias.DTOs.Requests;

public record FijarFechaFinLicenciaRequest(Guid LicenciaID, Guid DocenteID, DateTime FechaFin);
