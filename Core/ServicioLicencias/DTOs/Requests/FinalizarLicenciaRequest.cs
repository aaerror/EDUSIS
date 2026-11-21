namespace Core.ServicioLicencias.DTOs.Requests;

public record FinalizarLicenciaRequest(Guid LicenciaID, Guid DocenteID);
