using Domain.Alumno;

namespace Infrastructure.Shared
{
    public interface IUnitOfWork : IDisposable
    {
        IAlumnoRepository Alumnos { get; }
        void GuardarCambios();
    }
}