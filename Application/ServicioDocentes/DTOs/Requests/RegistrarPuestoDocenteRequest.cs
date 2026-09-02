namespace Core.ServicioDocentes.DTOs.Requests;

public record RegistrarPuestoDocenteRequest(Guid DocenteID, string Posicion, string Estado, DateTime FechaInicio, DateTime? FechaFin);