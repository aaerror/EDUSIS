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
}
