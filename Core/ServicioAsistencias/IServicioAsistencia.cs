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
	Task<PlanillaAsistenciaResponse> ConsultarPlanillaAsync(ConsultarPlanillaRequest request);
	Task<int> ContarFaltasAsync(ContarFaltasRequest request);
}
