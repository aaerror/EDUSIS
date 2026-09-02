using Domain.Shared;

namespace Domain.Alumnos.DomainEvents;

public record AlumnoDesinscriptoDomainEvent(Guid AlumnoID) : IDomainEvent;