using Core.Shared;
using Core.Shared.Documentos;

namespace Core.ServicioDocumentos;

internal class ServicioDocumento : IServicio, IServicioDocumento
{
	private readonly IGeneradorDocumentos _generadorDocumentos;

	public ServicioDocumento(IGeneradorDocumentos generadorDocumentos)
	{
		_generadorDocumentos = generadorDocumentos;
	}

	public async Task GenerarCertificadoAlumnoRegular()
	{
		try
		{
			var request = new CertificadoAlumnoRegularRequest(NombreCompleto: "Juan Perez",
															  DNI: "33998731",
															  FechaIngreso: DateTime.Today.Date,
															  Grado: "1",
															  NivelEducativo: "Secundaria");

			await _generadorDocumentos.GenerarCertificadoAlumnoRegularAsync(request);
		}
		catch (Exception ex)
		{
			throw;
		}
	}
}
