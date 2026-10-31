using Domain.Cursantes;
using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Cursante"/> (<see cref="ICursanteRepository"/>) sobre <c>List&lt;Cursante&gt;</c>.</summary>
public sealed class CursanteRepositorioFake : RepositorioEnMemoria<Cursante>, ICursanteRepository
{
	public Task<bool> ExisteInscripcionActivaAsync(Guid alumnoID, CicloLectivo cicloLectivo) =>
		Task.FromResult(_entidades.Any(x => x.AlumnoID.Equals(alumnoID) && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null));

	public Task<int> ContarCursantesDeDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo) =>
		Task.FromResult(_entidades.Count(x => x.DivisionID.Equals(divisionID) && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null));

	// Todas las inscripciones de la división: misma semántica que el repositorio real.
	public Task<int> ContarInscripcionesDeDivisionAsync(Guid divisionID) =>
		Task.FromResult(_entidades.Count(x => x.DivisionID.Equals(divisionID)));

	public Task<Cursante?> BuscarInscripcionActivaAsync(Guid alumnoID, CicloLectivo cicloLectivo) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.AlumnoID.Equals(alumnoID) && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null));

	// Matrícula vigente de la división: misma semántica que el repositorio real.
	public Task<IReadOnlyCollection<Cursante>> BuscarPorDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo) =>
		Task.FromResult((IReadOnlyCollection<Cursante>)_entidades
			.Where(x => x.DivisionID.Equals(divisionID) && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null)
			.ToList());

	// Matrícula vigente del ciclo lectivo: misma semántica que el repositorio real.
	public Task<IReadOnlyCollection<Cursante>> BuscarPorCicloLectivoAsync(CicloLectivo cicloLectivo) =>
		Task.FromResult((IReadOnlyCollection<Cursante>)_entidades
			.Where(x => x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null)
			.ToList());

	public Task<IReadOnlyCollection<Cursante>> BuscarInscripcionesDelAlumnoAsync(Guid alumnoID) =>
		Task.FromResult((IReadOnlyCollection<Cursante>)_entidades
			.Where(x => x.AlumnoID.Equals(alumnoID))
			.OrderBy(x => x.FechaInicio)
			.ToList());

	public Task<IReadOnlyCollection<Cursante>> BuscarInscripcionesDelCicloAsync(Guid alumnoID, CicloLectivo cicloLectivo) =>
		Task.FromResult((IReadOnlyCollection<Cursante>)_entidades
			.Where(x => x.AlumnoID.Equals(alumnoID) && x.CicloLectivo.Periodo == cicloLectivo.Periodo)
			.OrderBy(x => x.FechaInicio)
			.ToList());

	public Task<IReadOnlyCollection<Guid>> BuscarInscriptosEnFechaAsync(Guid divisionID, DateTime fecha) =>
		Task.FromResult((IReadOnlyCollection<Guid>)_entidades
			.Where(x => x.DivisionID.Equals(divisionID) &&
				x.FechaInicio.Date <= fecha.Date &&
				(x.FechaFin == null || x.FechaFin.Value.Date >= fecha.Date))
			.Select(x => x.Id)
			.ToList());
}
