using Domain.Licencias;

namespace Core.ServicioLicencias.DTOs.Responses;

// Articulo viaja como enumerado y no como texto: la pantalla consulta su PlazoLegal y no tiene
// cómo convertir el nombre (Enumerados es interno del dominio).
// Vencida: aprobada, con la fecha de fin pasada y sin finalizar; la pantalla la marca para que
// alguien registre el reintegro.
public record LicenciaResponse(Guid LicenciaID, Guid DocenteID, Articulo Articulo, string Estado, int Dias, DateTime FechaInicio, DateTime? FechaFin, string? Observacion, bool Vencida = false);
