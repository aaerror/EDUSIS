using Domain.Shared;

namespace Domain.Licencias.DomainEvents;

public record LicenciaRechazadaEvent(Guid LicenciaID, Guid DocenteID) : IDomainEvent;