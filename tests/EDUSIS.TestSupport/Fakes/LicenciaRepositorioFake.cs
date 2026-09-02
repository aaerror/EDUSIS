using Domain.Licencias;

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
}
