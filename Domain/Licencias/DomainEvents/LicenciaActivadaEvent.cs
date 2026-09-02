using Domain.Shared;

namespace Domain.Licencias.DomainEvents;

public record LicenciaActivadaEvent(Guid LicenciaID, Guid DocenteID) : IDomainEvent;