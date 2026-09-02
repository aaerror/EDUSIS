using Domain.Docentes.Puestos;
using Domain.Personas;

namespace Domain.Docentes;

public interface IDocenteRepository : IPersonaRepository<Docente>
{
	Task<Docente?> BuscarDocentePorIDConPuestosAsync(Guid docenteID);
	Task<Docente?> BuscarDocentePorIDConLicenciasAsync(Guid docenteID);

	Task<IReadOnlyCollection<Docente>> BuscarSegunNombreCompletoAsync(string nombreCompleto);

	Task<bool> EsCuilInvalidoAsync(string cuil);
	Task<bool> EsLegajoInvalidoAsync(string legajo);

	Task<IReadOnlyCollection<Puesto>> PuestosPorDocenteAsync(Guid docenteID);
}