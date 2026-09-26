using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;

namespace Core.ServicioDivisiones;

public interface IServicioDivision
{
	Task<IReadOnlyCollection<DivisionResponse>> ListarDivisionesAsync(ListarDivisionesRequest request);
	Task AgregarDivisionAsync(AgregarDivisionRequest request);
	Task EliminarDivisionAsync(EliminarDivisionRequest request);
	Task AsignarPreceptorAsync(RegistrarPreceptorRequest request);
	Task QuitarPreceptorAsync(EliminarPreceptorRequest request);
}
