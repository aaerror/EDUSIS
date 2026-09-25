using Domain.Divisiones;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class DivisionRepository : Repository<Division>, IDivisionRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;


	public DivisionRepository(EdusisDBContext context)
		: base(context) { }

	public async Task<bool> ExisteDivisionConDescripcionAsync(Guid cursoID, string descripcion) =>
		await _context.Divisiones.AnyAsync(x => x.CursoID == cursoID && x.Descripcion == descripcion);

	public async Task<bool> ExistePreceptorAsignadoAsync(Guid docenteID) =>
		await _context.Divisiones.AnyAsync(x => x.Preceptor == docenteID);

	public async Task<IReadOnlyCollection<string>> DescripcionesDelCursoAsync(Guid cursoID) =>
		await _context.Divisiones
			.Where(x => x.CursoID == cursoID)
			.Select(x => x.Descripcion)
			.ToListAsync();

	public async Task<IReadOnlyCollection<Division>> DivisionesDelCursoAsync(Guid cursoID) =>
		await Consulta()
			.Where(x => x.CursoID == cursoID)
			.ToListAsync();

	public async Task<Division?> BuscarPorCursoYDescripcionAsync(Guid cursoID, string descripcion) =>
		await Consulta()
			.FirstOrDefaultAsync(x => x.CursoID == cursoID && x.Descripcion == descripcion);
}
