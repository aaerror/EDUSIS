using Domain.Asistencias;

namespace Core.ServicioAsistencias.DTOs.Responses;

public record RegistroAsistenciaResponse(Guid CursanteID, TipoAsistencia Tipo, TimeSpan? Minutos, string? Observacion);
