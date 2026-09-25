using Domain.Licencias;
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
}