using Domain.Divisiones;
using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Division"/> (<see cref="IDivisionRepository"/>) sobre <c>List&lt;Division&gt;</c>.</summary>
public sealed class DivisionRepositorioFake : RepositorioEnMemoria<Division>, IDivisionRepository
{
	public Task<bool> ExisteDivisionConDescripcionAsync(Guid cursoID, string descripcion) =>
		Task.FromResult(_entidades.Any(x => x.CursoID.Equals(cursoID) && x.Descripcion == descripcion));

	public Task<bool> ExistePreceptorAsignadoAsync(Guid docenteID) =>
		Task.FromResult(_entidades.Any(x => x.Preceptor.Equals(docenteID)));

	public Task<IReadOnlyCollection<string>> DescripcionesDelCursoAsync(Guid cursoID) =>
		Task.FromResult((IReadOnlyCollection<string>)_entidades
			.Where(x => x.CursoID.Equals(cursoID))
			.Select(x => x.Descripcion)
			.ToList());

	public Task<IReadOnlyCollection<Division>> DivisionesDelCursoAsync(Guid cursoID) =>
		Task.FromResult((IReadOnlyCollection<Division>)_entidades
			.Where(x => x.CursoID.Equals(cursoID))
			.ToList());

	public Task<Division?> BuscarPorCursoYDescripcionAsync(Guid cursoID, string descripcion) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.CursoID.Equals(cursoID) && x.Descripcion == descripcion));
}
