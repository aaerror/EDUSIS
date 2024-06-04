using Domain.Materias;
using Domain.Materias.Horarios;
using Domain.Materias.SituacionRevistaDocente;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class MateriaRepository : Repository<Materia>, IMateriaRepository
{
    private EdusisDBContext _context => Context as EdusisDBContext;


    public MateriaRepository(EdusisDBContext context)
        : base(context)
    {}

    public bool NombreDuplicado(Guid cursoID, string descripcion) =>
        _context.Materias.Where(x => x.CursoID.Equals(cursoID))
            .Any(x => x.Descripcion.ToLower().Trim().Equals(descripcion.ToLower().Trim()));

    public IEnumerable<SituacionRevista> HistoricoSituacionRevista(Guid cursoID, Guid materiaID) =>
        _context.Materias
            .AsNoTracking()
            .Where(x => x.CursoID.Equals(cursoID) && x.Id.Equals(materiaID))
            .Select(x => x.Profesores)
            .FirstOrDefault();

    public IEnumerable<Horario> BuscarHorarios(Guid cursoID, Guid materiaID) =>
        _context.Materias
            .AsNoTracking()
            .Where(x => x.CursoID.Equals(cursoID) && x.Id.Equals(materiaID))
            .Select(x => x.Horarios)
            .FirstOrDefault();
}