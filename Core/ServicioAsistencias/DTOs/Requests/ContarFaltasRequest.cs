using Domain.Asistencias;

namespace Core.ServicioAsistencias.DTOs.Requests;

public record ContarFaltasRequest(Guid CursanteID, TipoAsistencia Tipo);
