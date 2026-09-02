namespace Core.ServicioDocentes.DTOs.Requests;

public record EliminarPuestoDocenteRequest(Guid DocenteID, Guid PuestoID);