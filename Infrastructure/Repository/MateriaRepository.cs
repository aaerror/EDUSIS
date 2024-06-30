using Domain.Materias;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class MateriaRepository : Repository<Materia>, IMateriaRepository
{
    private EdusisDBContext _context => Context as EdusisDBContext;


    public MateriaRepository(EdusisDBContext context)
        : base(context) { }

    public bool NombreDuplicadoEnCurso(Guid cursoID, Guid? materiaID, string descripcion)
    {
        if (materiaID.HasValue)
        {
            return _context.Materias
                .Where(x => x.CursoID.Equals(cursoID) && !x.Id.Equals(materiaID) && string.Equals(x.Descripcion.Trim().ToLower(), descripcion.Trim().ToLower()))
                .Any();
        }
        
        return _context.Materias
            .Where(x => x.CursoID.Equals(cursoID) && string.Equals(x.Descripcion.Trim().ToLower(), descripcion.Trim().ToLower()))
            .Any();
    }

    public Materia? BuscarMateria(Guid cursoID, Guid materiaID) =>
        _context.Materias
            .Find(cursoID, materiaID);

    public IEnumerable<Materia> MateriasSegunCurso(Guid cursoID) =>
        _context.Materias
            .Include(x => x.Horarios)
            .Include(x => x.Docentes)
            .Where(x => x.CursoID.Equals(cursoID))
            .ToList();
}