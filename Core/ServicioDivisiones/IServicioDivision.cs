using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;

namespace Core.ServicioDivisiones;

public interface IServicioDivision
{
	Task<IReadOnlyCollection<DivisionResponse>> ListarDivisionesAsync(ListarDivisionesRequest request);
	Task<string> ProponerDescripcionDivisionAsync(ProponerDescripcionRequest request);
	Task AgregarDivisionAsync(AgregarDivisionRequest request);
	Task EliminarDivisionAsync(EliminarDivisionRequest request);
	Task<ImpactoEliminacionDivisionResponse> ConsultarImpactoEliminacionDivisionAsync(EliminarDivisionRequest request);
	Task AsignarPreceptorAsync(RegistrarPreceptorRequest request);
	Task ModificarPreceptorAsync(ModificarPreceptorRequest request);
	Task QuitarPreceptorAsync(EliminarPreceptorRequest request);
}
