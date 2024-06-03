using Domain.Alumnos;
using Domain.Cursos;
using Domain.Docentes;
using Domain.Materias;
using Domain.Usuarios;
using Infrastructure.Extensions;
using Infrastructure.Repository;
using Infrastructure.Shared;
using MediatR;

namespace Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly EdusisDBContext _context;
    private readonly IMediator _mediator;

    public IAlumnoRepository Alumnos { get; private set; }
    public IDocenteRepository Docentes { get; private set; }
    public ICursoRepository Cursos { get; private set; }
    public IMateriaRepository Materias { get; private set; }
    public IUsuarioRepository Usuarios { get; private set; }


    public UnitOfWork(EdusisDBContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
        Alumnos = new AlumnoRepository(_context);
        Docentes = new DocenteRepository(_context);
        Cursos = new CursoRepository(_context);
        Materias = new MateriaRepository(_context);
        Usuarios = new UsuarioRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<int> GuardarCambiosAsync()
    {
        await MediatrExtension.DispatchDomainEventsAsync(_mediator, _context);

        return await _context.SaveChangesAsync();
    }
}
