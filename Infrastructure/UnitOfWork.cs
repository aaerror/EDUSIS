using Domain.Alumno;
using Infrastructure.Shared;

namespace Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly EdusisDBContext _context = new EdusisDBContext();
    public IAlumnoRepository Alumnos { get; private set; }


    public UnitOfWork()
    {
        Alumnos = new AlumnoRepository(_context);
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
