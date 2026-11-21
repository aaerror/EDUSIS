using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Responses;

namespace Core.ServicioLicencias;

public interface IServicioLicencia
{
	Task<IReadOnlyCollection<LicenciaResponse>> BuscarLicenciasSegunDocenteAsync(DocenteIDRequest request);

	/// <summary>
	/// Las pendientes de todos los docentes que la sesión puede resolver: sin el permiso de
	/// resolución académica, las de exámenes, estudio encomendado y otras causas no aparecen.
	/// </summary>
	Task<IReadOnlyCollection<LicenciaPendienteResponse>> ListarLicenciasPendientesAsync();

	Task SolicitarLicencia(NuevaSolicitudLicenciaRequest request);
	Task AprobarLicencia(ActualizarEstadoLicenciaRequest request);
	Task RechazarLicencia(ActualizarEstadoLicenciaRequest request);
	Task ModificarLicencia(ModificarLicenciaRequest request);
	Task EliminarLicencia(EliminarLicenciaRequest request);

	#region Licencia aprobada
	Task FinalizarLicencia(FinalizarLicenciaRequest request);
	Task ExtenderLicencia(ExtenderLicenciaRequest request);
	Task FijarFechaFinLicencia(FijarFechaFinLicenciaRequest request);
	Task DeclararLicenciaIndefinida(DeclararLicenciaIndefinidaRequest request);
	#endregion
}
