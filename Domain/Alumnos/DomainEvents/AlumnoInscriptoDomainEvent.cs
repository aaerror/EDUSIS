using Domain.Shared;

namespace Domain.Alumnos.DomainEvents;

public record AlumnoInscriptoDomainEvent(Guid AlumnoID) : IDomainEvent;