using Domain.Shared;

namespace Domain.Licencias.DomainEvents;

public record LicenciaFinalizadaEvent(Guid LicenciaID, Guid DocenteID) : IDomainEvent;