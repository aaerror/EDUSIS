namespace Core.ServicioMaterias.DTOs.Responses;

public record HorarioResponse(string Turno, string Dia, TimeOnly HoraInicio, TimeOnly HoraFin);