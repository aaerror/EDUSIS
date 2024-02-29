using Domain.Cursos.Divisiones;
using Domain.Shared;

namespace Domain.Cursos;

public interface ICursoRepository : IRepository<Curso>
{
    IEnumerable<Division> BuscarDivisiones(Guid unCurso);
    IEnumerable<Curso> CursosConDivisionesMaterias();
    void CambiarAlumnoDeCurso(Guid alumnoId, Guid nuevoCursoId, Guid nuevaDivisionId);
}
