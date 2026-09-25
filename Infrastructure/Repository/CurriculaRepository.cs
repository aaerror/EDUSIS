using Domain.Curriculas;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class CurriculaRepository : Repository<Curricula>, ICurriculaRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;

	public CurriculaRepository(EdusisDBContext context)
		: base(context) { }

	public async Task<Curricula?> BuscarCurriculaAsync(Guid unCurso, Guid unaCurricula) =>
		await Consulta().FirstOrDefaultAsync(x => x.CursoID == unCurso && x.Id == unaCurricula);

	public async Task<IReadOnlyCollection<Curricula>> CurriculasSegunCursoAsync(Guid unCurso) =>
		await Consulta().Where(x => x.CursoID == unCurso).ToListAsync();
}