using Domain.Cursos;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class CursoRepository : Repository<Curso>, ICursoRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;

	public CursoRepository(EdusisDBContext context)
		: base(context) { }

	public async Task<bool> ExisteCursoAsync(Grado grado, NivelEducativo nivelEducativo) =>
		await _context.Cursos.AnyAsync(x => x.Grado == grado && x.NivelEducativo == nivelEducativo);
}
