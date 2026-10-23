namespace Core.Shared.Documentos;

/// <summary>
/// Puerto de renderizado de documentación. Lo define <c>Core</c> y lo implementa la
/// infraestructura: los casos de uso resuelven QUÉ dice cada documento y acá se resuelve CÓMO se
/// imprime.
/// </summary>
/// <remarks>
/// Ningún método recibe una ruta ni devuelve un archivo: todos devuelven
/// <see cref="DocumentoGenerado"/>. Ver las notas de ese tipo.
/// </remarks>
public interface IGeneradorDocumentos
{
	#region Documentación
	Task<DocumentoGenerado> GenerarCertificadoAlumnoRegularAsync(CertificadoAlumnoRegularDocumento documento);

	Task<DocumentoGenerado> GenerarConstanciaEscolaridadAsync(ConstanciaEscolaridadDocumento documento);

	Task<DocumentoGenerado> GenerarCertificadoAsistenciaAsync(CertificadoAsistenciaDocumento documento);

	Task<DocumentoGenerado> GenerarConstanciaMateriasAsync(ConstanciaMateriasDocumento documento);

	Task<DocumentoGenerado> GenerarBoletinCalificacionesAsync(BoletinCalificacionesDocumento documento);
	#endregion

	#region Reportes
	Task<DocumentoGenerado> GenerarReporteAsync(ReporteTabularDocumento documento);
	#endregion
}
