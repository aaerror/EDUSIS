using Domain.Shared;

namespace Domain.Catedras.DomainEvents;

public record CatedraSinDocenteEnFuncionesEvent(Guid CatedraID) : IDomainEvent;
