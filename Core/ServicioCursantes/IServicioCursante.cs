using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes.DTOs.Responses;

namespace Core.ServicioCursantes;

public interface IServicioCursante
{
	Task InscribirCursanteAsync(RegistrarCursanteRequest request);
	Task<IReadOnlyCollection<CursanteResponse>> ListarCursantesAsync(BuscarListadoRequest request);
	Task<Guid> RegistrarCalificacionAsync(CrearCalificationRequest request);
	Task QuitarCalificacionAsync(EliminarCalificacionRequest request);
	Task<Guid> RegistrarInasistenciaAExamenAsync(RegistrarInasistenciaRequest request);
	Task ModificarObservacionCalificacionAsync(ModificarObservacionCalificacionRequest request);
	Task<IReadOnlyCollection<CalificacionResponse>> ListarCalificacionesAsync(ListarCalificacionesRequest request);
}
