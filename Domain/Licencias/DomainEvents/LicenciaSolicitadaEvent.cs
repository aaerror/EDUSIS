using Domain.Shared;

namespace Domain.Licencias.DomainEvents;

public record LicenciaSolicitadaEvent(Guid LicenciaID, Guid DocenteID) : IDomainEvent;