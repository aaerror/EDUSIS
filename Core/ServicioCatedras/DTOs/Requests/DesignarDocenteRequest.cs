namespace Core.ServicioCatedras.DTOs.Requests;

public record DesignarDocenteRequest(Guid CatedraID, Guid DocenteID, string Cargo, DateTime FechaInicio, DateTime? FechaFin, Guid? ReemplazaA);
