namespace Core.ServicioCatedras.DTOs.Requests;

public record AgregarHorarioRequest(Guid CatedraID, string Turno, string DiaSemana, TimeOnly HoraInicio, int DuracionHoraCatedra);
