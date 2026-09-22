using Domain.Shared;

namespace Domain.Materias.DomainEvents;

public record MateriaRegistradaEvent(Guid MateriaID) : IDomainEvent;