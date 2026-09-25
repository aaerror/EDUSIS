using Domain.Shared;

namespace Domain.Catedras;

public interface ICatedraRepository : IRepository<Catedra>
{
	Task<Catedra?> BuscarPorMateriaYDivisionAsync(Guid unaMateria, Guid unaDivision);
	Task<IReadOnlyCollection<Catedra>> CatedrasSegunMateriaAsync(Guid unaMateria);
	Task<IReadOnlyCollection<Catedra>> CatedrasSegunDivisionAsync(Guid unaDivision);
	Task<IReadOnlyCollection<Catedra>> CatedrasSegunDocenteAsync(Guid unDocente);
	Task<bool> ExisteCatedraAsync(Guid unaMateria, Guid unaDivision);
}
