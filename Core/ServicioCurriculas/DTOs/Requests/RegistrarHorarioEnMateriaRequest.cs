namespace Core.ServicioCurriculas.DTOs.Requests;

public record RegistrarHorarioEnMateriaRequest(Guid CursoID, Guid MateriaID, int Turno, int Dia, string HoraInicio, int Duracion);