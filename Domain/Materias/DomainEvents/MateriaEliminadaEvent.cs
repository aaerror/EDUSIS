using Domain.Shared;

namespace Domain.Materias.DomainEvents;

public record MateriaEliminadaEvent(Guid MateriaID) : IDomainEvent;
