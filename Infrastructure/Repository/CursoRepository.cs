using Domain.Cursos;
using Domain.Cursos.Divisiones;
using Infrastructure.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class CursoRepository : Repository<Curso>, ICursoRepository
{
    private EdusisDBContext _context => Context as EdusisDBContext;


    public CursoRepository(EdusisDBContext context)
        : base(context) { }

    public void CambiarAlumnoDeCurso(Guid alumnoId, Guid nuevoCursoId, Guid nuevaDivisionId)
    {
        throw new NotImplementedException();
    }

    public override Curso BuscarPorID(Guid id)
    {
        return Context.Set<Curso>()
                      .Include(x => x.Materias)
                      .Include(x => x.Divisiones)
                      .Where(x => x.Id.Equals(id))
                      .FirstOrDefault();
    }

    public override void ActualizarDatos(Curso entity)
    {
        Context.Update(entity);
    }

    public IEnumerable<Division> BuscarDivisiones(Guid unCurso)
    {
        return _context.Cursos.Include(x => x.Divisiones)
                              .Where(x => x.Id.Equals(unCurso))
                              .Select(x => x.Divisiones)
                              .FirstOrDefault();
    }

    public IEnumerable<Curso> CursosConDivisionesMaterias()
    {
        return _context.Cursos.Include(x => x.Divisiones)
                              .Include(x => x.Materias)
                              .OrderByDescending(x => x.Descripcion)
                              .ThenBy(x => x.NivelEducativo);
    }
}
