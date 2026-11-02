using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas.DTOs.Responses;

namespace Core.ServicioCurriculas;

public interface IServicioCurricula
{
	Task<IReadOnlyCollection<CurriculaResponse>> ListarCurriculasSegunCursoAsync(CursoRequest request);

	/// <summary>
	/// Cuánto de la currícula está ocupado y cuánto queda, en espacios curriculares y en horas cátedra
	/// semanales. Es lo que permite mostrar el tope antes de chocarse con él.
	/// </summary>
	Task<OcupacionCurricularResponse> ConsultarOcupacionAsync(CurriculaRequest request);

	Task RegistrarCurriculaAsync(RegistrarCurriculaRequest request);

	Task DesafectarCurriculaAsync(CurriculaRequest request);
}
