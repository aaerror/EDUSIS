using Domain.Cursos.Divisiones;
using Domain.Shared;

namespace Domain.Cursos;

public interface ICursoRepository : IRepository<Curso>
{
    Curso CursoConDivisiones(Guid cursoID);

    void CambiarAlumnoDeCurso(Guid alumnoID, Guid nuevoCursoID, Guid nuevaDivisionID);

    IEnumerable<Division> DivisionesDelCurso(Guid unCurso);
    IEnumerable<Curso> CursosConDivisiones();
}
