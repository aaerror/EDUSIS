using Domain.Materias.SituacionRevistaDocente;
using Domain.Shared;

namespace Domain.Materias;

public interface IMateriaRepository : IRepository<Materia>
{
    bool NombreDuplicado(Guid cursoID, string descripcion);

    IReadOnlyCollection<SituacionRevista> HistoricoSituacionRevista(Guid cursoID, Guid materiaID);
}