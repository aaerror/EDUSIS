using Domain.Docentes;
using Domain.Docentes.Puestos;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class DocenteRepository : Repository<Docente>, IDocenteRepository
{
	private EdusisDBContext _context => Context as EdusisDBContext;


	public DocenteRepository(EdusisDBContext context)
		: base(context) { }

	public async Task<Docente?> BuscarDocentePorIDConPuestosAsync(Guid docenteID)
	{
		var docente = await _context.Docentes
			.Include(x => x.Puestos)
			.Where(x => x.Id.Equals(docenteID))
			.FirstOrDefaultAsync();

		return docente;
	}

	public async Task<Docente?> BuscarDocentePorIDConLicenciasAsync(Guid docenteID)
	{
		var docente = await _context.Docentes
			.Include(x => x.Licencias)
			.Where(x => x.Id.Equals(docenteID))
			.FirstOrDefaultAsync();

		return docente;
	}

	public async Task<IReadOnlyCollection<Docente>> BuscarSegunNombreCompletoAsync(string nombreCompleto) =>
		await _context.Docentes
			.Where(x =>
				EF.Functions.Like(x.DatosPersonales.Nombre.Trim().ToLower(), $"%{ nombreCompleto.Trim().ToLower() }%") ||
				EF.Functions.Like(x.DatosPersonales.Apellido.Trim().ToLower(), $"%{ nombreCompleto.Trim().ToLower() }%"))
			.ToListAsync();

	public async Task<bool> ExisteIDAsync(Guid id) =>
		await _context.Docentes.AnyAsync(x => x.Id.Equals(id));

	public async Task<bool> EsDocumentoInvalidoAsync(string documento) =>
		await _context.Docentes.AnyAsync(x => EF.Functions.Like(x.DatosPersonales.Documento, documento));
		//await _context.Docentes.AnyAsync(x => string.Equals(x.DatosPersonales.Documento, documento, StringComparison.InvariantCultureIgnoreCase));

	public async Task<bool> EsCuilInvalidoAsync(string cuil) =>
		await _context.Docentes.AnyAsync(x => EF.Functions.Like(x.CUIL, cuil));
		// await _context.Docentes.AnyAsync(x => string.Equals(x.CUIL, cuil, StringComparison.InvariantCultureIgnoreCase));

	public async Task<bool> EsLegajoInvalidoAsync(string legajo) =>
		await _context.Docentes.AnyAsync(x => EF.Functions.Like(x.Legajo, legajo));
		// await _context.Docentes.AnyAsync(x => string.Equals(x.Legajo, legajo, StringComparison.InvariantCultureIgnoreCase));

	public async Task<IReadOnlyCollection<Puesto>> PuestosPorDocenteAsync(Guid docenteID) =>
		await _context.Docentes
			.Where(x => x.Id.Equals(docenteID))
			.SelectMany(x => x.Puestos)
			.ToListAsync();
}