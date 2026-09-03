namespace Core.ServicioLicencias.DTOs.Requests;

public record ModificarLicenciaRequest(Guid LicenciaID, Guid DocenteID, string Articulo, DateTime FechaInicio, int Dias, string? Observacion);