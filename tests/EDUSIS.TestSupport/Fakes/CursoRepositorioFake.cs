using Domain.Cursos;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Curso"/> (<see cref="ICursoRepository"/>) sobre <c>List&lt;Curso&gt;</c>.</summary>
public sealed class CursoRepositorioFake : RepositorioEnMemoria<Curso>, ICursoRepository
{
	public Curso CursoConDivisiones(Guid cursoID) =>
		_entidades.FirstOrDefault(x => x.Id.Equals(cursoID))
			?? throw new InvalidOperationException($"No se sembró un curso con Id {cursoID} en el fake.");

	public void CambiarAlumnoDeCurso(Guid alumnoID, Guid nuevoCursoID, Guid nuevaDivisionID) =>
		Afectadas++;

	public IEnumerable<Division> DivisionesDelCurso(Guid unCurso) =>
		CursoConDivisiones(unCurso).Divisiones;

	public IEnumerable<Curso> CursosConDivisiones() =>
		_entidades;
}
