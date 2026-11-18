using Domain.Shared;

namespace Domain.Materias;

public interface IMateriaRepository : IRepository<Materia>
{
	Task<bool> ExisteNombreMateriaEnCurriculaAsync(Guid unaCurricula, string descripcion);
	Task<bool> ExisteNombreMateriaEnCurriculaAsync(Guid unaCurricula, string descripcion, Guid excluirMateria);
	Task<IReadOnlyCollection<Materia>> BuscarMateriasSegunCurriculaAsync(Guid unaCurricula);
	Task<int> TotalHorasCatedraSegunCurriculaAsync(Guid unaCurricula);

	/// <summary>Nombre de cada materia pedida. Un identificador que no existe no aparece en el resultado.</summary>
	Task<IReadOnlyDictionary<Guid, string>> DescripcionesAsync(IEnumerable<Guid> materiasID);
	Task<int> TotalEspaciosSegunCurriculaAsync(Guid unaCurricula);
}
