using Domain.Shared;

namespace Domain.Asistencias.DomainEvents;

public record PlanillaAsistenciaReabiertaEvent(Guid PlanillaID, Guid DivisionID, DateTime Fecha) : IDomainEvent;
