using Domain.Shared;

namespace Domain.Catedras;

public interface ICatedraRepository : IRepository<Catedra>
{
	Task<Catedra?> BuscarPorMateriaYDivisionAsync(Guid unaMateria, Guid unaDivision);
	Task<IEnumerable<Catedra>> CatedrasSegunMateriaAsync(Guid unaMateria);
	Task<IEnumerable<Catedra>> CatedrasSegunDivisionAsync(Guid unaDivision);
	Task<IEnumerable<Catedra>> CatedrasSegunDocenteAsync(Guid unDocente);
	Task<bool> ExisteCatedraAsync(Guid unaMateria, Guid unaDivision);
}
