using Domain.Docentes.Puestos;
using Domain.Personas;
using Domain.Shared;

namespace Domain.Docentes;

public interface IDocenteRepository : IPersonaRepository<Docente>
{
	Task<IReadOnlyCollection<Docente>> BuscarSegunNombreCompletoAsync(string nombreCompleto);
	Task<IReadOnlyCollection<Docente>> BuscarActivosAsync();
	Task<IReadOnlyCollection<Docente>> BuscarSegunPosicionAsync(Posicion posicion);
	Task<bool> ExisteDocenteConLegajoAsync(Guid docenteID, string legajo);
	Task<bool> ExisteCuilAsync(string cuil);
	Task<bool> ExisteLegajoAsync(string legajo);

	/// <summary>
	/// Nombre completo de cada docente pedido, activo o dado de baja: los listados históricos (las
	/// designaciones de una cátedra) tienen que nombrar también a quien ya no trabaja en la escuela.
	/// Un identificador que no existe no aparece en el resultado.
	/// </summary>
	Task<IReadOnlyDictionary<Guid, string>> NombresAsync(IEnumerable<Guid> docentesID);
}