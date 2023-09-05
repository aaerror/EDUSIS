using Domain.Alumnos;

namespace Infrastructure.Shared;

public interface IUnitOfWork : IDisposable
{
    IAlumnoRepository Alumnos { get; }
    void GuardarCambios();
}