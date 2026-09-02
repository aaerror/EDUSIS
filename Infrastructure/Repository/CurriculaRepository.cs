using Domain.Curriculas;
using Domain.Cursos;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class CurriculaRepository : Repository<Curricula>, ICurriculaRepository
{
	private EdusisDBContext _context => Context as EdusisDBContext;


	public CurriculaRepository(EdusisDBContext context)
		: base(context) {}

	public async Task<Curricula?> BuscarCurriculaAsync(Guid unCurso, Guid unaCurricula) =>
		await _context.Curriculas
			.Include(x => x.Materias)
				.ThenInclude(x => x.Docentes)
				.ThenInclude(x => x.Periodo)
			.Where(x => x.CursoID.Equals(unCurso) && x.Id.Equals(unaCurricula))
			.FirstOrDefaultAsync();

	public async Task<IEnumerable<Curricula>> CurriculasSegunCursoAsync(Guid unCurso) =>
		await _context.Curriculas
			.Include(x => x.Materias)
				.ThenInclude(x => x.Docentes)
				.ThenInclude(x => x.Periodo)
			.Where(x => x.CursoID.Equals(unCurso))
			.ToListAsync();
}