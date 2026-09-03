using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;

namespace Core.ServicioCursos;

public interface IServicioCurso
{
	Task<IReadOnlyCollection<CursoResponse>> ListarCursosAsync();
	Task RegistrarCurso(RegistrarCursoRequest request);
	Task EliminarCurso(EliminarCursoRequest request);

	#region Divisiones
	Task<IReadOnlyCollection<DivisionResponse>> BuscarDivisionesAsync(Guid unCurso);
	/*IReadOnlyCollection<CursanteResponse> BuscarListado(BuscarListadoRequest request);
	void InscribirAlumnoEnDivision(CrearCursanteRequest request);*/
	Task RegistrarPreceptorEnDivision(RegistrarPreceptorRequest request);
	Task EliminarPreceptorDeDivision(EliminarPreceptorRequest request);
	Task AgregarDivisionAlCurso(Guid unCurso);
	Task QuitarDivisiosDelCurso(EliminarDivisionRequest request);
	#endregion

	#region Calificación
	Task RegistrarCalificacion(CrearCalificationRequest request);
	#endregion
}