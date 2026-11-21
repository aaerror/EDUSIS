using Domain.Licencias;

namespace Core.ServicioLicencias.DTOs.Responses;

// Una fila de la bandeja de pendientes: la licencia más el nombre de su docente, que vive en otro
// agregado y la bandeja no tiene cómo resolver.
public record LicenciaPendienteResponse(Guid LicenciaID, Guid DocenteID, string Docente, Articulo Articulo, int Dias, DateTime FechaInicio, DateTime? FechaFin, string? Observacion);
