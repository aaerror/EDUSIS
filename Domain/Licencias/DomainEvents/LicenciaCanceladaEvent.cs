using Domain.Shared;

namespace Domain.Licencias.DomainEvents;

public record LicenciaCanceladaEvent(Guid LicenciaID, Guid DocenteID) : IDomainEvent;