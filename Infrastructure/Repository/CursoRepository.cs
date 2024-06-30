using Domain.Cursos;
using Domain.Cursos.Divisiones;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class CursoRepository : Repository<Curso>, ICursoRepository
{
    private EdusisDBContext _context => Context as EdusisDBContext;


    public CursoRepository(EdusisDBContext context)
        : base(context) { }


    public Curso CursoConDivisiones(Guid cursoID) =>
        _context.Cursos
            .Include(x => x.Divisiones)
            .Where(x => x.Id.Equals(cursoID))
            .FirstOrDefault();

    public IEnumerable<Curso> CursosConDivisiones() =>
       _context.Cursos
            .Include(x => x.Divisiones)
            .OrderByDescending(x => x.Grado)
            .ThenBy(x => x.NivelEducativo);

    public IEnumerable<Division> DivisionesDelCurso(Guid unCurso) =>
        _context.Cursos
            .AsNoTracking()
            .Include(x => x.Divisiones)
            .Where(x => x.Id.Equals(unCurso))
            .Select(x => x.Divisiones)
            .FirstOrDefault();

    public void CambiarAlumnoDeCurso(Guid alumnoId, Guid nuevoCursoId, Guid nuevaDivisionId)
    {
        throw new NotImplementedException();
    }
}
