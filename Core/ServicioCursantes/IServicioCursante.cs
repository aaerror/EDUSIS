using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes.DTOs.Responses;

namespace Core.ServicioCursantes;

public interface IServicioCursante
{
	Task InscribirCursanteAsync(RegistrarCursanteRequest request);
	Task ModificarCondicionDeRecursanteAsync(ModificarCondicionRecursanteRequest request);
	Task RegistrarBajaDeCursanteAsync(RegistrarBajaCursanteRequest request);
	Task CambiarDeDivisionAsync(CambiarDivisionCursanteRequest request);
	Task<IReadOnlyCollection<CursanteResponse>> ListarCursantesAsync(BuscarListadoRequest request);
	Task<IReadOnlyCollection<InscripcionHistoricaResponse>> ConsultarHistorialDeDivisionesAsync(ConsultarHistorialRequest request);
	Task<Guid> RegistrarCalificacionAsync(CrearCalificationRequest request);
	Task QuitarCalificacionAsync(EliminarCalificacionRequest request);
	Task<Guid> RegistrarInasistenciaAExamenAsync(RegistrarInasistenciaRequest request);
	Task ModificarObservacionCalificacionAsync(ModificarObservacionCalificacionRequest request);
	Task<IReadOnlyCollection<CalificacionResponse>> ListarCalificacionesAsync(ListarCalificacionesRequest request);
}
