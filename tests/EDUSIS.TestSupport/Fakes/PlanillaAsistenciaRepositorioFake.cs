using Domain.Asistencias;
using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="PlanillaAsistencia"/> (<see cref="IPlanillaAsistenciaRepository"/>) sobre <c>List&lt;PlanillaAsistencia&gt;</c>.</summary>
public sealed class PlanillaAsistenciaRepositorioFake : RepositorioEnMemoria<PlanillaAsistencia>, IPlanillaAsistenciaRepository
{
	public Task<bool> ExistePlanillaAsync(Guid divisionID, DateTime fecha) =>
		Task.FromResult(_entidades.Any(x => x.DivisionID.Equals(divisionID) && x.Fecha == fecha.Date));

	public Task<PlanillaAsistencia?> BuscarPorDivisionYFechaAsync(Guid divisionID, DateTime fecha) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.DivisionID.Equals(divisionID) && x.Fecha == fecha.Date));

	public Task<int> ContarFaltasAsync(Guid cursanteID, TipoAsistencia tipo) =>
		Task.FromResult(_entidades
			.SelectMany(p => p.Registros)
			.Count(r => r.CursanteID.Equals(cursanteID) && r.Tipo == tipo));
}
