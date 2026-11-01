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

	/// <summary>
	/// Ciclos lectivos habilitados, del más reciente al más antiguo, para que la pantalla los
	/// ofrezca en lugar de pedirlos tipeados.
	/// </summary>
	/// <remarks>
	/// El mismo catálogo lo expone <c>IServicioDocumento</c>. Está en los dos a propósito y no en
	/// un servicio común: cada uno exige su propio permiso —<c>GenerarReportes</c> acá,
	/// <c>GenerarDocumentos</c> allá—, y un único punto compartido obligaría a elegir uno de los
	/// dos o a dejar la lista sin guarda.
	/// </remarks>
	Task<IReadOnlyCollection<string>> ListarCiclosLectivosAsync();
}
