namespace Core.ServicioCursantes.DTOs.Requests;

public record CambiarDivisionCursanteRequest(Guid AlumnoID, string Periodo, Guid DivisionDestinoID, DateTime FechaCambio);
