using Domain.Catedras;
using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Catedra"/> (<see cref="ICatedraRepository"/>) sobre <c>List&lt;Catedra&gt;</c>.</summary>
public sealed class CatedraRepositorioFake : RepositorioEnMemoria<Catedra>, ICatedraRepository
{
	public Task<Catedra?> BuscarPorMateriaYDivisionAsync(Guid unaMateria, Guid unaDivision) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.MateriaID.Equals(unaMateria) && x.DivisionID.Equals(unaDivision)));

	public Task<IReadOnlyCollection<Catedra>> CatedrasSegunMateriaAsync(Guid unaMateria) =>
		Task.FromResult((IReadOnlyCollection<Catedra>)_entidades
			.Where(x => x.MateriaID.Equals(unaMateria))
			.ToList());

	public Task<IReadOnlyCollection<Catedra>> CatedrasSegunDivisionAsync(Guid unaDivision) =>
		Task.FromResult((IReadOnlyCollection<Catedra>)_entidades
			.Where(x => x.DivisionID.Equals(unaDivision))
			.ToList());

	public Task<IReadOnlyCollection<Catedra>> CatedrasSegunDocenteAsync(Guid unDocente) =>
		Task.FromResult((IReadOnlyCollection<Catedra>)_entidades
			.Where(x => x.SituacionesRevista.Any(s => s.DocenteID.Equals(unDocente)))
			.ToList());

	public Task<bool> ExisteCatedraAsync(Guid unaMateria, Guid unaDivision) =>
		Task.FromResult(_entidades.Any(x => x.MateriaID.Equals(unaMateria) && x.DivisionID.Equals(unaDivision)));
}
