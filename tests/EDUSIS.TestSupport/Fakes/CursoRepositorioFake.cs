using Domain.Cursos;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Curso"/> (<see cref="ICursoRepository"/>) sobre <c>List&lt;Curso&gt;</c>.</summary>
public sealed class CursoRepositorioFake : RepositorioEnMemoria<Curso>, ICursoRepository
{
}
