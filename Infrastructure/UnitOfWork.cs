using Domain.Alumnos;
using Domain.Cursos;
using Domain.Docentes;
using Infrastructure.Repository;
using Infrastructure.Shared;

namespace Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly EdusisDBContext _context = new EdusisDBContext();
    public IAlumnoRepository Alumnos { get; private set; }
    public IDocenteRepository Docentes { get; private set; }
    public ICursoRepository Cursos { get; private set; }


    public UnitOfWork()
    {
        Alumnos = new AlumnoRepository(_context);
        Docentes = new DocenteRepository(_context);
        Cursos = new CursoRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public void GuardarCambios()
    {
        _context.SaveChanges();
    }
}
