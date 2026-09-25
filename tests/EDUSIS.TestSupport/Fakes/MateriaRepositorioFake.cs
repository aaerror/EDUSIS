using Domain.Materias;
using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Materia"/> (<see cref="IMateriaRepository"/>) sobre <c>List&lt;Materia&gt;</c>.</summary>
public sealed class MateriaRepositorioFake : RepositorioEnMemoria<Materia>, IMateriaRepository
{
	public Task<bool> ExisteNombreMateriaEnCurriculaAsync(Guid unaCurricula, string descripcion) =>
		Task.FromResult(_entidades.Any(x => x.CurriculaID.Equals(unaCurricula) && x.Descripcion.ToLower() == descripcion.Trim().ToLower()));

	public Task<bool> ExisteNombreMateriaEnCurriculaAsync(Guid unaCurricula, string descripcion, Guid excluirMateria) =>
		Task.FromResult(_entidades.Any(x => x.CurriculaID.Equals(unaCurricula) && x.Descripcion.ToLower() == descripcion.Trim().ToLower() && !x.Id.Equals(excluirMateria)));

	public Task<IReadOnlyCollection<Materia>> BuscarMateriasSegunCurriculaAsync(Guid unaCurricula) =>
		Task.FromResult((IReadOnlyCollection<Materia>)_entidades
			.Where(x => x.CurriculaID.Equals(unaCurricula))
			.ToList());

	public Task<int> TotalHorasCatedraSegunCurriculaAsync(Guid unaCurricula) =>
		Task.FromResult(_entidades
			.Where(x => x.CurriculaID.Equals(unaCurricula))
			.Sum(x => x.HorasCatedra));

	public Task<int> TotalEspaciosSegunCurriculaAsync(Guid unaCurricula) =>
		Task.FromResult(_entidades
			.Count(x => x.CurriculaID.Equals(unaCurricula)));
}
