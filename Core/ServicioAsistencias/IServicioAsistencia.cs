using Core.ServicioAsistencias.DTOs.Requests;
using Core.ServicioAsistencias.DTOs.Responses;

namespace Core.ServicioAsistencias;

public interface IServicioAsistencia
{
	Task<PlanillaAsistenciaResponse> AbrirPlanillaAsync(AbrirPlanillaRequest request);
	Task MarcarPresenteAsync(MarcarPresenteRequest request);
	Task MarcarAusenciaAsync(MarcarAusenciaRequest request);
	Task MarcarInasistenciaAsync(MarcarInasistenciaRequest request);
	Task MarcarTardanzaAsync(MarcarTardanzaRequest request);
	Task IncorporarCursanteAsync(IncorporarCursanteRequest request);
	Task CerrarPlanillaAsync(CerrarPlanillaRequest request);
	Task ReabrirPlanillaAsync(ReabrirPlanillaRequest request);
	Task EliminarPlanillaAsync(EliminarPlanillaRequest request);
	Task<PlanillaAsistenciaResponse> ConsultarPlanillaAsync(ConsultarPlanillaRequest request);
	Task<int> ContarFaltasAsync(ContarFaltasRequest request);

	/// <summary>
	/// Las planillas ya cargadas de una división dentro del rango, de la más reciente a la más vieja.
	/// Devuelve una colección vacía si no hay ninguna: no tener planillas en un período no es un error.
	/// </summary>
	Task<IReadOnlyCollection<PlanillaResumenResponse>> ListarPlanillasAsync(ListarPlanillasRequest request);

	/// <summary>
	/// El acumulado de asistencia de una división en un período, con una fila por alumno ordenada de
	/// más faltas a menos.
	/// </summary>
	Task<ResumenAsistenciaResponse> ConsultarResumenAsync(ConsultarResumenRequest request);
}
