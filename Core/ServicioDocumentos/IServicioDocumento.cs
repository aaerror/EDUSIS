using Core.ServicioDocumentos.DTOs.Requests;
using Core.Shared.Documentos;

namespace Core.ServicioDocumentos;

/// <summary>
/// Emisión de documentación de alumno. Todas las operaciones exigen
/// <c>Permiso.GenerarDocumentos</c> y devuelven el documento en memoria: dónde se guarda lo
/// decide la capa de presentación.
/// </summary>
public interface IServicioDocumento
{
	Task<DocumentoGenerado> GenerarCertificadoAlumnoRegularAsync(EmitirDocumentoAlumnoRequest request);

	Task<DocumentoGenerado> GenerarConstanciaEscolaridadAsync(EmitirDocumentoAlumnoRequest request);

	Task<DocumentoGenerado> GenerarCertificadoAsistenciaAsync(EmitirCertificadoAsistenciaRequest request);

	Task<DocumentoGenerado> GenerarConstanciaMateriasAprobadasAsync(EmitirDocumentoAlumnoRequest request);

	Task<DocumentoGenerado> GenerarConstanciaMateriasPendientesAsync(EmitirDocumentoAlumnoRequest request);

	Task<DocumentoGenerado> GenerarBoletinCalificacionesAsync(EmitirDocumentoAlumnoRequest request);

	/// <summary>
	/// Ciclos lectivos habilitados, del más reciente al más antiguo, para que la pantalla los
	/// ofrezca en lugar de pedirlos tipeados.
	/// </summary>
	/// <remarks>
	/// El mismo catálogo lo expone <c>IServicioReporte</c>. Está en los dos a propósito y no en un
	/// servicio común: cada uno exige su propio permiso —<c>GenerarDocumentos</c> acá,
	/// <c>GenerarReportes</c> allá—, y un único punto compartido obligaría a elegir uno de los dos
	/// o a dejar la lista sin guarda.
	/// </remarks>
	Task<IReadOnlyCollection<string>> ListarCiclosLectivosAsync();
}
