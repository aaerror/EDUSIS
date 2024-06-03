using Domain.Materias;
using Domain.Materias.SituacionRevistaDocente;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class MateriaRepository : Repository<Materia>, IMateriaRepository
{
    private EdusisDBContext _context => Context as EdusisDBContext;


    public MateriaRepository(EdusisDBContext context)
        : base(context)
    {}

    public bool NombreDuplicado(Guid cursoID, string descripcion) => _context.Materias.Where(x => x.CursoID.Equals(cursoID))
                                                                                      .Any(x => x.Descripcion.ToLower().Trim().Equals(descripcion.ToLower().Trim()));

    public IReadOnlyCollection<SituacionRevista> HistoricoSituacionRevista(Guid cursoID, Guid materiaID) => _context.Materias.Include(x => x.Profesores)
                                                                                                                             .Where(x => x.CursoID.Equals(cursoID) && x.Id.Equals(materiaID))
                                                                                                                             .FirstOrDefault().Profesores;
}