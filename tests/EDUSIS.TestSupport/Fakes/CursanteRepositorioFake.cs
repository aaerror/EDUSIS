using Domain.Cursantes;
using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Cursante"/> (<see cref="ICursanteRepository"/>) sobre <c>List&lt;Cursante&gt;</c>.</summary>
public sealed class CursanteRepositorioFake : RepositorioEnMemoria<Cursante>, ICursanteRepository
{
	public Task<bool> ExisteInscripcionAsync(Guid alumnoID, CicloLectivo cicloLectivo) =>
		Task.FromResult(_entidades.Any(x => x.AlumnoID.Equals(alumnoID) && x.CicloLectivo.Periodo == cicloLectivo.Periodo));

	public Task<int> ContarCursantesDeDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo) =>
		Task.FromResult(_entidades.Count(x => x.DivisionID.Equals(divisionID) && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null));

	public Task<Cursante?> BuscarInscripcionActivaAsync(Guid alumnoID, CicloLectivo cicloLectivo) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.AlumnoID.Equals(alumnoID) && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null));

	public Task<IReadOnlyCollection<Cursante>> BuscarPorDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo) =>
		Task.FromResult((IReadOnlyCollection<Cursante>)_entidades
			.Where(x => x.DivisionID.Equals(divisionID) && x.CicloLectivo.Periodo == cicloLectivo.Periodo)
			.ToList());

	public Task<IReadOnlyCollection<Guid>> BuscarInscriptosEnFechaAsync(Guid divisionID, DateTime fecha) =>
		Task.FromResult((IReadOnlyCollection<Guid>)_entidades
			.Where(x => x.DivisionID.Equals(divisionID) &&
				x.FechaInicio.Date <= fecha.Date &&
				(x.FechaFin == null || x.FechaFin.Value.Date >= fecha.Date))
			.Select(x => x.Id)
			.ToList());
}
