using Domain.Shared;

namespace Domain.Docentes.DomainEvents;

public record DocenteDesafectadoDomainEvent(Guid DocenteID) : IDomainEvent;