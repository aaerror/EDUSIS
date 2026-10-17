using Core.Shared;
using Core.Shared.Documentos;
using Core.Shared.Seguridad;
using Domain.Usuarios;

namespace Core.ServicioDocumentos;

internal class ServicioDocumento : IServicio, IServicioDocumento
{
	private readonly IGeneradorDocumentos _generadorDocumentos;
	private readonly ISesionActual _sesion;

	public ServicioDocumento(IGeneradorDocumentos generadorDocumentos, ISesionActual sesion)
	{
		_sesion = sesion;
		_generadorDocumentos = generadorDocumentos;
	}

	public async Task GenerarCertificadoAlumnoRegular()
	{
		try
		{
			_sesion.Exigir(Permiso.GestionarAlumnos);

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
