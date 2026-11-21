using Domain.Shared;

namespace Domain.Licencias;

public interface ILicenciaRepository : IRepository<Licencia>
{
	Task<IReadOnlyCollection<Licencia>> BuscarLicenciasDeDocenteAsync(Guid docenteID);
	Task<Licencia?> BuscarPorIDYDocenteAsync(Guid licenciaID, Guid docenteID);

	/// <summary>Las licencias pendientes de aprobación de todos los docentes, de la más antigua a la más reciente.</summary>
	Task<IReadOnlyCollection<Licencia>> BuscarPendientesAsync();

	/// <summary>
	/// Si el docente ya tiene otra licencia que ocupa algún día de <paramref name="periodo"/>, según
	/// <see cref="Licencia.SeSuperponeCon"/> y <see cref="Licencia.OcupaSuPeriodo"/>.
	/// </summary>
	/// <param name="licenciaExcluida">La licencia que se está modificando, que no choca consigo misma.</param>
	Task<bool> ExisteSuperpuestaAsync(Guid docenteID, RangoFechas periodo, Guid licenciaExcluida);
}
