using Domain.Shared;

namespace Domain.Asistencias.DomainEvents;

public record PlanillaAsistenciaCerradaEvent(Guid PlanillaID, Guid DivisionID, DateTime Fecha) : IDomainEvent;
