using Core.Shared.Documentos;

namespace Infrastructure.Documentos;

internal class GeneradorDocumentos : IGeneradorDocumentos
{
	private readonly IPortableDocumentFormat _documento;

	public GeneradorDocumentos(IPortableDocumentFormat documento)
	{
		_documento = documento;
	}

	public async Task GenerarCertificadoAlumnoRegularAsync(CertificadoAlumnoRegularRequest request)
	{
		var certificadoAlumnoRegular = new CertificadoAlumnoRegular(request.NombreCompleto,
																	request.DNI,
																	request.FechaIngreso,
																	request.Grado,
																	request.NivelEducativo)
			.AgregarTituloDocumento("Certificado Alumno Regular", null)
			.EnsamblarDocumento();

		await _documento.Create("test.pdf", "Test", certificadoAlumnoRegular);
	}
}
