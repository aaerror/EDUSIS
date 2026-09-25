using Domain.Cursos;

namespace Infrastructure.Repository;

internal class CursoRepository : Repository<Curso>, ICursoRepository
{
	public CursoRepository(EdusisDBContext context)
		: base(context) { }
}
