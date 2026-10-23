using Core.ServicioReportes.DTOs.Requests;
using Core.Shared.Documentos;

namespace Core.ServicioReportes;

/// <summary>
/// Reportes institucionales. Todas las operaciones exigen <c>Permiso.GenerarReportes</c> y
/// devuelven el documento en memoria: dónde se guarda lo decide la capa de presentación.
/// </summary>
public interface IServicioReporte
{
	Task<DocumentoGenerado> GenerarReporteMatriculasAsync(ReportePorCicloRequest request);

	Task<DocumentoGenerado> GenerarReporteAsistenciasAsync(ReporteAsistenciasRequest request);

	Task<DocumentoGenerado> GenerarReporteCalificacionesAsync(ReportePorCicloRequest request);

	Task<DocumentoGenerado> GenerarReporteMateriasPendientesAsync(ReportePorCicloRequest request);

	Task<DocumentoGenerado> GenerarReporteTrayectoriasAsync(ReportePorCicloRequest request);
}
