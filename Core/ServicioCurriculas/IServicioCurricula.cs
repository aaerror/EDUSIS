using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas.DTOs.Responses;

namespace Core.ServicioCurriculas;

public interface IServicioCurricula
{
	#region Curriculas
	Task<IReadOnlyCollection<CurriculaResponse>> ListarCurriculasSegunCursoAsync(CursoRequest request);

	Task RegistrarCurriculaAsync(RegistrarCurriculaRequest request);

	Task DesafectarCurriculaAsync(CurriculaRequest request);
	#endregion

	#region Materias
	Task<IReadOnlyCollection<MateriaResponse>> ListarMateriasSegunCurriculaAsync(ListarMateriasSegunCurriculaRequest request);

	Task RegistrarMateria(RegistrarMateriaRequest request);
	Task ModificarMateriaAsync(ModificarMateriaRequest request);
	Task EliminarMateriaAsync(EliminarMateriaRequest request);
	#endregion

	#region Situación Revista
	Task RegistrarDocenteEnMateriaAsync(RegistrarDocenteEnMateriaRequest request);
	Task<IReadOnlyCollection<SituacionRevistaResponse>> ListarCargosDocenteSegunMateriaAsync(ListarCargosDocenteSegunMateriaRequest request);
	Task EstablecerDocenteDeAulaAsync(EstablecerDocenteDeAulaRequest request);
	Task RelevarDocenteDeFuncionesEnMateriaAsync(RelevarDocenteDeAulaRequest request);
	Task RescindirCargoDocenteDeMateriaAsync(RescindirCargoDocenteRequest request);
	Task EliminarCargoDocenteAsync(EliminarCargoDocenteRequest request);
	#endregion
}