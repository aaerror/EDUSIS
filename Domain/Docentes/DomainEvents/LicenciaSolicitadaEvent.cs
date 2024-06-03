using Domain.Shared;

namespace Domain.Docentes.DomainEvents;

public record LicenciaSolicitadaEvent : IDomainEvent
{
    public Guid DocenteID { get; init; }


    public LicenciaSolicitadaEvent(Guid docenteID)
    {
        DocenteID = docenteID;
    }
}
