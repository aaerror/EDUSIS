namespace Core.ServicioLicencias.DTOs.Requests;

public record NuevaSolicitudLicenciaRequest(Guid DocenteID, string Articulo, DateTime FechaInicio, int Dias, string? Observacion);