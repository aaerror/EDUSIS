namespace Core.ServicioCursantes.DTOs.Responses;

/// <param name="Periodo">Nombre del miembro de <c>Periodo</c>: el tramo del ciclo lectivo.</param>
/// <param name="EsRecuperatorio">Si es el segundo intento dentro de ese período.</param>
/// <param name="Aprobado">
/// Si ESTA instancia alcanza el umbral por sí sola. No dice que la materia esté aprobada: eso
/// depende de todas las instancias juntas y lo decide <c>RendimientoAcademico</c>.
/// </param>
public record CalificacionResponse(Guid CalificacionID, Guid MateriaID, string Materia, DateTime Fecha, string Periodo, bool EsRecuperatorio, bool Rindio, double? Nota, bool Aprobado, string? Observacion);
