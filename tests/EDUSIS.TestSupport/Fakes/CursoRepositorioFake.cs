using Domain.Cursos;
using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Curso"/> (<see cref="ICursoRepository"/>) sobre <c>List&lt;Curso&gt;</c>.</summary>
public sealed class CursoRepositorioFake : RepositorioEnMemoria<Curso>, ICursoRepository
{
	public Task<bool> ExisteCursoAsync(Grado grado, NivelEducativo nivelEducativo) =>
		Task.FromResult(_entidades.Any(x => x.Grado == grado && x.NivelEducativo == nivelEducativo));
}
