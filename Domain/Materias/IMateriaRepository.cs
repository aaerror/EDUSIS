using Domain.Materias.Horarios;
using Domain.Materias.SituacionRevistaDocente;
using Domain.Shared;

namespace Domain.Materias;

public interface IMateriaRepository : IRepository<Materia>
{
    bool NombreDuplicado(Guid cursoID, string descripcion);
    IEnumerable<SituacionRevista> HistoricoSituacionRevista(Guid cursoID, Guid materiaID);
    IEnumerable<Horario> BuscarHorarios(Guid cursoID, Guid materiaID);
}