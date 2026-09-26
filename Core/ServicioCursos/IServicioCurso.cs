using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;

namespace Core.ServicioCursos;

public interface IServicioCurso
{
	Task<IReadOnlyCollection<CursoResponse>> ListarCursosAsync();
	Task RegistrarCurso(RegistrarCursoRequest request);
	Task EliminarCurso(EliminarCursoRequest request);
}
