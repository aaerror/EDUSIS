using Domain.Divisiones;
using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Division"/> (<see cref="IDivisionRepository"/>) sobre <c>List&lt;Division&gt;</c>.</summary>
public sealed class DivisionRepositorioFake : RepositorioEnMemoria<Division>, IDivisionRepository
{
	/// <summary>
	/// Fuerza que <see cref="ExisteDivisionConDescripcionAsync"/> devuelva <see langword="true"/>
	/// sin importar el estado del repo. Desde DIV-1 la letra la elige el usuario, así que una
	/// descripción repetida es alcanzable sembrando la división y se prueba sin la bandera; lo que
	/// la bandera sigue cubriendo es la carrera entre dos altas concurrentes, que registran la
	/// misma letra entre la propuesta de <c>Division.Siguiente</c> y el alta.
	/// </summary>
	public bool ForzarExisteDivisionConDescripcion { get; set; }

	public Task<bool> ExisteDivisionConDescripcionAsync(Guid cursoID, string descripcion) =>
		Task.FromResult(ForzarExisteDivisionConDescripcion || _entidades.Any(x => x.CursoID.Equals(cursoID) && x.Descripcion == descripcion));

	public Task<bool> ExistePreceptorAsignadoAsync(Guid docenteID) =>
		Task.FromResult(_entidades.Any(x => x.Preceptor.Equals(docenteID)));

	public Task<IReadOnlyCollection<Division>> BuscarPorPreceptorAsync(Guid docenteID) =>
		Task.FromResult((IReadOnlyCollection<Division>)_entidades
			.Where(x => x.Preceptor.Equals(docenteID))
			.ToList());

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
