namespace Core.ServicioDivisiones.DTOs.Requests;

/// <remarks>
/// No lleva ciclo lectivo: el borrado cascadea y se lleva las inscripciones de
/// <strong>todos</strong> los ciclos, así que un período en el request no acotaría nada y sería un
/// campo que la pantalla cree que significa algo.
///
/// <c>CursoID</c> se conserva: el servicio verifica con él que el curso exista, que es lo que el
/// drill-down Cursos → Divisiones da por hecho.
/// </remarks>
public record EliminarDivisionRequest(Guid CursoID, Guid DivisionID);
