using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas.DTOs.Responses;

namespace Core.ServicioCurriculas;

public interface IServicioCurricula
{
	Task<IReadOnlyCollection<CurriculaResponse>> ListarCurriculasSegunCursoAsync(CursoRequest request);

	Task RegistrarCurriculaAsync(RegistrarCurriculaRequest request);

	Task DesafectarCurriculaAsync(CurriculaRequest request);
}
