using Domain.Personas;

namespace Domain.Docentes;

public interface IDocenteRepository : IPersonaRepository<Docente>
{
    bool EsCuilInvalido(string cuil);
    bool EsLegajoInvalido(string legajo);
    IReadOnlyCollection<Puesto> Puestos(Guid docenteID);
}
