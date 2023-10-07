using Domain.Shared;

namespace Domain.Cursos;

public interface ICursoRepository : IRepository<Curso>
{
    void CambiarAlumnoDeCurso(Guid alumnoId, Guid nuevoCursoId, Guid nuevaDivisionId);
}
