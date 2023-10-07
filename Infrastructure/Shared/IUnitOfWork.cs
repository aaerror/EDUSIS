using Domain.Alumnos;
using Domain.Cursos;

namespace Infrastructure.Shared;

public interface IUnitOfWork : IDisposable
{
    IAlumnoRepository Alumnos { get; }
    ICursoRepository Cursos { get; }

    void GuardarCambios();
}