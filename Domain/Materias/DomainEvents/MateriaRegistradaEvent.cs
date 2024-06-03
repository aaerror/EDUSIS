using Domain.Shared;

namespace Domain.Materias.DomainEvents;

public record MateriaRegistradaEvent : IDomainEvent
{
    public Guid MateriaID = Guid.Empty;


    public MateriaRegistradaEvent(Guid materiaID)
    {
        MateriaID = materiaID;
    }
}
