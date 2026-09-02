using Domain.Curriculas;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Curricula"/> (<see cref="ICurriculaRepository"/>) sobre <c>List&lt;Curricula&gt;</c>.</summary>
public sealed class CurriculaRepositorioFake : RepositorioEnMemoria<Curricula>, ICurriculaRepository
{
	public Task<Curricula?> BuscarCurriculaAsync(Guid unCurso, Guid unaCurricula) =>
		Task.FromResult(_entidades.FirstOrDefault(x =>
			x.Id.Equals(unaCurricula) && x.CursoID.Equals(unCurso)));

	public Task<IEnumerable<Curricula>> CurriculasSegunCursoAsync(Guid unCurso) =>
		Task.FromResult(_entidades.Where(x => x.CursoID.Equals(unCurso)));
}
