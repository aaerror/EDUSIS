using Domain.Catedras;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class CatedraRepository : Repository<Catedra>, ICatedraRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;


	public CatedraRepository(EdusisDBContext context)
		: base(context) { }

	protected override IQueryable<Catedra> Consulta() =>
		_context.Catedras
			.Include(x => x.Horarios)
			.Include(x => x.SituacionesRevista);

	public async Task<Catedra?> BuscarPorMateriaYDivisionAsync(Guid unaMateria, Guid unaDivision) =>
		await Consulta()
			.FirstOrDefaultAsync(x => x.MateriaID == unaMateria && x.DivisionID == unaDivision);

	public async Task<IReadOnlyCollection<Catedra>> CatedrasSegunMateriaAsync(Guid unaMateria) =>
		await Consulta()
			.Where(x => x.MateriaID == unaMateria)
			.ToListAsync();

	public async Task<IReadOnlyCollection<Catedra>> CatedrasSegunDivisionAsync(Guid unaDivision) =>
		await Consulta()
			.Where(x => x.DivisionID == unaDivision)
			.ToListAsync();

	public async Task<IReadOnlyCollection<Catedra>> CatedrasSegunDocenteAsync(Guid unDocente) =>
		await Consulta()
			.Where(x => x.SituacionesRevista.Any(s => s.DocenteID == unDocente))
			.ToListAsync();

	public async Task<bool> ExisteCatedraAsync(Guid unaMateria, Guid unaDivision) =>
		await _context.Catedras.AnyAsync(x => x.MateriaID == unaMateria && x.DivisionID == unaDivision);
}
