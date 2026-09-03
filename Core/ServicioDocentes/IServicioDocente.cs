using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;

namespace Core.ServicioDocentes;

public interface IServicioDocente
{
	Task<PerfilDocenteResponse> VerPerfilDocenteAsync(DocenteIDRequest request);

	//LegajoDocenteResponse BuscarLegajoDocentePorDNI(string documento);
	Task<IReadOnlyCollection<LegajoDocenteResponse>> ListarDocentesActivosAsync();
	Task<IReadOnlyCollection<LegajoDocenteResponse>> BuscarDocenteSegunNombreCompletoAsync(NombreCompletoRequest request);

	Task<LegajoDocenteResponse> MostrarLegajoDocenteAsync(DocenteIDRequest request);
	Task RegistrarDocenteAsync(RegistrarDocenteRequest request);
	Task ActualizarContacto(CambiarContactoRequest request);
	Task ActualizarDomicilio(CambiarDomicilioRequest request);
	Task ActualizarSexo(CambiarSexoRequest request);
	void QuitarDocente(DocenteIDRequest request);

	/*
	IReadOnlyCollection<LicenciaResponse> BuscarLicencias(DocenteIDRequest request);
	LicenciaResponse RegistrarLicencia(RegistrarLicenciaDocenteRequest request);
	LicenciaResponse ModificarLicencia(EditarLicenciaRequest request);
	void AprobarLicencia(EditarEstadoLicenciaRequest request);
	void CancelarLicencia(EditarEstadoLicenciaRequest request);
	*/

	#region Puesto
	Task<IReadOnlyCollection<PuestoResponse>> ListarPuestosDocentesAsync(DocenteIDRequest request);
	Task AgregarPuestoDocenteAsync(RegistrarPuestoDocenteRequest request);
	Task EditarPuestoDocenteAsync(EditarPuestoDocenteRequest request);
	Task RevocarPuestoDocenteAsync(RevocarPuestoDocenteRequest request);
	Task EliminarPuestoDocenteAsync(EliminarPuestoDocenteRequest request);
	#endregion
}