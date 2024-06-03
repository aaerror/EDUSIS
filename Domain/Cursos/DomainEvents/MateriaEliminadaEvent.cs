using Domain.Shared;

namespace Domain.Cursos.DomainEvents;

public record MateriaEliminadaEvent(Guid materiaID) : IDomainEvent;
