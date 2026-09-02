namespace Core.ServicioDocentes.DTOs.Requests;

public record RevocarPuestoDocenteRequest(Guid DocenteID, Guid PuestoID, DateTime? FechaFin);