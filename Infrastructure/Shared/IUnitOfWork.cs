using Domain.Alumnos;
using Domain.Cursos;
using Domain.Docentes;
using Domain.Materias;
using Domain.Usuarios;

namespace Infrastructure.Shared;

public interface IUnitOfWork : IDisposable
{
    IAlumnoRepository Alumnos { get; }

    IDocenteRepository Docentes { get; }

    ICursoRepository Cursos { get; } 

    IMateriaRepository Materias { get; }

    IUsuarioRepository Usuarios { get; }

    Task<int> GuardarCambiosAsync();
}