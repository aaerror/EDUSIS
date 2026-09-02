using Domain.Shared;

namespace Domain.Curriculas.DomainEvents;

public record MateriaRegistradaEvent(Guid MateriaID) : IDomainEvent;