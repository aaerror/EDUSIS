namespace Core.ServicioDivisiones.DTOs.Requests;

/// <remarks>
/// Llevaba un tercer campo <c>string CicloLectivo</c> que se quitó el 2026-11-06 al pasar el borrado
/// de prohibir a cascadear: lo consumía la guarda de matrícula vigente
/// (<c>ContarCursantesDeDivisionAsync</c>), que ya no existe. El borrado se lleva las inscripciones
/// de <strong>todos</strong> los ciclos, así que un período en el request no acotaría nada y sería un
/// campo que la pantalla cree que significa algo — el mismo defecto que UI-45 registró para
/// <c>ReportePorCicloRequest</c>.
///
/// <c>CursoID</c> se conserva: el servicio verifica con él que el curso exista, que es lo que el
/// drill-down Cursos → Divisiones da por hecho.
/// </remarks>
public record EliminarDivisionRequest(Guid CursoID, Guid DivisionID);
