using Domain.Personas;
using Domain.Shared;

namespace Domain.Docentes;

public interface IDocenteRepository : IPersonaRepository<Docente>
{
	Task<IReadOnlyCollection<Docente>> BuscarSegunNombreCompletoAsync(string nombreCompleto);
	Task<IReadOnlyCollection<Docente>> BuscarActivosAsync();
	Task<bool> ExisteDocenteConLegajoAsync(Guid docenteID, string legajo);
	Task<bool> EsCuilInvalidoAsync(string cuil);
	Task<bool> EsLegajoInvalidoAsync(string legajo);
}