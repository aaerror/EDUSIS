using Domain.Shared;

namespace Domain.Materias;

public interface IMateriaRepository : IRepository<Materia>
{
    bool NombreDuplicadoEnCurso(Guid cursoID, Guid? materiaID, string descripcion);
    Materia? BuscarMateria(Guid cursoID, Guid materiaID);
    IEnumerable<Materia> MateriasSegunCurso(Guid cursoID);
}