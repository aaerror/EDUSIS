namespace Core.ServicioDivisiones.DTOs.Responses;

public record AlumnoConFaltasResponse(Guid AlumnoID, string NombreCompleto, int Faltas, decimal PorcentajeAsistencia);
