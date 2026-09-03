namespace Core.Shared.Documentos;

public interface IGeneradorDocumentos
{
	Task GenerarCertificadoAlumnoRegularAsync(CertificadoAlumnoRegularRequest request);
}
