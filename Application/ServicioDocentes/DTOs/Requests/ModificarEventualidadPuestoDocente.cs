namespace Core.ServicioDocentes.DTOs.Requests;

public record EditarPuestoDocenteRequest(Guid DocenteID, Guid PuestoID, string Posicion, DateTime FechaInicio, DateTime? FechaFin);