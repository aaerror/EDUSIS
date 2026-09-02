using Domain.Shared;

namespace Domain.Licencias;

public interface ILicenciaRepository : IRepository<Licencia>
{
	Task<IReadOnlyCollection<Licencia>> BuscarLicenciasDeDocenteAsync(Guid docenteID);
}