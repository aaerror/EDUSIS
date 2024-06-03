using Domain.Shared;

namespace Domain.Alumnos.DomainEvents;

public record AlumnoRegistradoDomainEvent : IDomainEvent
{
    public Guid AlumnoID { get; }


    public AlumnoRegistradoDomainEvent(Guid alumnoID)
    {
        AlumnoID = alumnoID;
    }
}
