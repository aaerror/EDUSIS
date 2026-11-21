namespace Core.ServicioLicencias.DTOs.Requests;

public record ExtenderLicenciaRequest(Guid LicenciaID, Guid DocenteID, int Dias);
