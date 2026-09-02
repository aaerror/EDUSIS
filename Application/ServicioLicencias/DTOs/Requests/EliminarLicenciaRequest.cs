namespace Core.ServicioLicencias.DTOs.Requests;

public record EliminarLicenciaRequest(Guid LicenciaID, Guid DocenteID);