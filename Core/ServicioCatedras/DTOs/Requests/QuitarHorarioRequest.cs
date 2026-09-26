namespace Core.ServicioCatedras.DTOs.Requests;

public record QuitarHorarioRequest(Guid CatedraID, string Turno, string DiaSemana, TimeOnly HoraInicio, int DuracionHoraCatedra);
