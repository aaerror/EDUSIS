using Domain.Shared;

namespace Domain.Materias;

public interface IMateriaRepository : IRepository<Materia>
{
	Task<bool> ExisteNombreMateriaEnCurriculaAsync(Guid unaCurricula, string descripcion);
	Task<bool> ExisteNombreMateriaEnCurriculaAsync(Guid unaCurricula, string descripcion, Guid excluirMateria);
	Task<IReadOnlyCollection<Materia>> BuscarMateriasSegunCurriculaAsync(Guid unaCurricula);
	Task<int> TotalHorasCatedraSegunCurriculaAsync(Guid unaCurricula);
	Task<int> TotalEspaciosSegunCurriculaAsync(Guid unaCurricula);
}
