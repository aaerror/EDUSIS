using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Responses;

namespace Core.ServicioLicencias;

public interface IServicioLicencia
{
	Task<IReadOnlyCollection<LicenciaResponse>> BuscarLicenciasSegunDocenteAsync(DocenteIDRequest request);
	Task SolicitarLicencia(NuevaSolicitudLicenciaRequest request);
	Task AprobarLicencia(ActualizarEstadoLicenciaRequest request);
	Task RechazarLicencia(ActualizarEstadoLicenciaRequest request);
	Task ModificarLicencia(ModificarLicenciaRequest request);
	Task EliminarLicencia(EliminarLicenciaRequest request);
}