using Domain.Licencias;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class LicenciaRepository : Repository<Licencia>, ILicenciaRepository
{
	private EdusisDBContext _context => Context as EdusisDBContext;


	public LicenciaRepository(EdusisDBContext context)
		: base(context) { }

	public async Task<IReadOnlyCollection<Licencia>> BuscarLicenciasDeDocenteAsync(Guid docenteID)
	{
		var licencias = await _context.Licencias
			.Where(x => x.DocenteID.Equals(docenteID))
			.ToListAsync();

		return licencias.AsReadOnly();
	}
}