using Domain.Docentes;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class DocenteRepository : PersonaRepository<Docente>, IDocenteRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;

	public DocenteRepository(EdusisDBContext context)
		: base(context) { }

	protected override IQueryable<Docente> Consulta() =>
		_context.Docentes.Include(x => x.Puestos);

	public async Task<IReadOnlyCollection<Docente>> BuscarSegunNombreCompletoAsync(string nombreCompleto) =>
		await Consulta()
			.Where(x =>
				EF.Functions.Like(x.DatosPersonales.Nombre.Trim().ToLower(), $"%{ nombreCompleto.Trim().ToLower() }%") ||
				EF.Functions.Like(x.DatosPersonales.Apellido.Trim().ToLower(), $"%{ nombreCompleto.Trim().ToLower() }%"))
			.ToListAsync();

	public async Task<IReadOnlyCollection<Docente>> BuscarActivosAsync() =>
		await Consulta().Where(x => x.Activo).ToListAsync();

	public async Task<bool> ExisteDocenteConLegajoAsync(Guid docenteID, string legajo) =>
		await _context.Docentes.AnyAsync(x => x.Id == docenteID && x.Legajo == legajo);

	public async Task<bool> EsCuilInvalidoAsync(string cuil) =>
		await _context.Docentes.AnyAsync(x => EF.Functions.Like(x.CUIL, cuil));

	public async Task<bool> EsLegajoInvalidoAsync(string legajo) =>
		await _context.Docentes.AnyAsync(x => EF.Functions.Like(x.Legajo, legajo));
}