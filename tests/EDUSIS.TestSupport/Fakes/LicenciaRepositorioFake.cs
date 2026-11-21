using Domain.Licencias;
using Domain.Shared;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Licencia"/> (<see cref="ILicenciaRepository"/>) sobre <c>List&lt;Licencia&gt;</c>.</summary>
public sealed class LicenciaRepositorioFake : RepositorioEnMemoria<Licencia>, ILicenciaRepository
{
	public Task<IReadOnlyCollection<Licencia>> BuscarLicenciasDeDocenteAsync(Guid docenteID)
	{
		IReadOnlyCollection<Licencia> licencias = _entidades
			.Where(x => x.DocenteID.Equals(docenteID))
			.ToList();

		return Task.FromResult(licencias);
	}

	public Task<Licencia?> BuscarPorIDYDocenteAsync(Guid licenciaID, Guid docenteID) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.Id.Equals(licenciaID) && x.DocenteID.Equals(docenteID)));

	public Task<IReadOnlyCollection<Licencia>> BuscarPendientesAsync()
	{
		IReadOnlyCollection<Licencia> pendientes = _entidades
			.Where(x => x.Estado == Estado.Pendiente)
			.OrderBy(x => x.Periodo.FechaInicio)
			.ToList();

		return Task.FromResult(pendientes);
	}

	public Task<bool> ExisteSuperpuestaAsync(Guid docenteID, RangoFechas periodo, Guid licenciaExcluida) =>
		Task.FromResult(_entidades.Any(x => x.DocenteID.Equals(docenteID)
										 && !x.Id.Equals(licenciaExcluida)
										 && x.OcupaSuPeriodo()
										 && x.SeSuperponeCon(periodo)));
}
