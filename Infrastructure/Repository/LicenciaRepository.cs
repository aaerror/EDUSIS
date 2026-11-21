using Domain.Licencias;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class LicenciaRepository : Repository<Licencia>, ILicenciaRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;

	public LicenciaRepository(EdusisDBContext context)
		: base(context) { }

	public async Task<IReadOnlyCollection<Licencia>> BuscarLicenciasDeDocenteAsync(Guid docenteID) =>
		await _context.Licencias
			.Where(x => x.DocenteID.Equals(docenteID))
			.ToListAsync();

	public async Task<Licencia?> BuscarPorIDYDocenteAsync(Guid licenciaID, Guid docenteID) =>
		await _context.Licencias.FirstOrDefaultAsync(x => x.Id == licenciaID && x.DocenteID == docenteID);

	public async Task<IReadOnlyCollection<Licencia>> BuscarPendientesAsync() =>
		await _context.Licencias
			.Where(x => x.Estado == Estado.Pendiente)
			.OrderBy(x => x.Periodo.FechaInicio)
			.ToListAsync();

	/// <remarks>
	/// Es <see cref="Licencia.SeSuperponeCon"/> y <see cref="Licencia.OcupaSuPeriodo"/> escritos para
	/// que EF los traduzca: los dos extremos son días de licencia y un fin nulo no termina. La
	/// comparación va por <c>.Date</c> porque <c>RangoFechas</c> normaliza el inicio pero no el fin.
	/// </remarks>
	public async Task<bool> ExisteSuperpuestaAsync(Guid docenteID, RangoFechas periodo, Guid licenciaExcluida)
	{
		var inicio = periodo.FechaInicio.Date;
		DateTime? fin = periodo.FechaFin?.Date;

		return await _context.Licencias
			.Where(x => x.DocenteID == docenteID && x.Id != licenciaExcluida && x.Estado != Estado.Rechazada)
			.AnyAsync(x => (fin == null || x.Periodo.FechaInicio <= fin)
						&& (x.Periodo.FechaFin == null || inicio <= x.Periodo.FechaFin.Value.Date));
	}
}
