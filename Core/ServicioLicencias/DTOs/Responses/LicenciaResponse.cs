using Domain.Licencias;

namespace Core.ServicioLicencias.DTOs.Responses;

// Articulo viaja como enumerado y no como texto: la pantalla consulta su PlazoLegal y no tiene
// cómo convertir el nombre (Enumerados es interno del dominio).
public record LicenciaResponse(Guid LicenciaID, Guid DocenteID, Articulo Articulo, string Estado, int Dias, DateTime FechaInicio, DateTime? FechaFin, string? Observacion);
