using Domain.Cursos;
using Infrastructure.Shared;

namespace Infrastructure;

public class CursoRepository : Repository<Curso>, ICursoRepository
{
    private EdusisDBContext _context => Context as EdusisDBContext;


    public CursoRepository(EdusisDBContext context)
        : base(context) { }

    public void CambiarAlumnoDeCurso(Guid alumnoId, Guid nuevoCursoId, Guid nuevaDivisionId)
    {
        throw new NotImplementedException();
    }
}
