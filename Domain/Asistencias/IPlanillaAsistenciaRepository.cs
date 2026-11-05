using Domain.Shared;

namespace Domain.Asistencias;

public interface IPlanillaAsistenciaRepository : IRepository<PlanillaAsistencia>
{
	Task<bool> ExistePlanillaAsync(Guid divisionID, DateTime fecha);

	Task<PlanillaAsistencia?> BuscarPorDivisionYFechaAsync(Guid divisionID, DateTime fecha);

	Task<int> ContarFaltasAsync(Guid cursanteID, TipoAsistencia tipo);

	/// <summary>
	/// Planillas de una división dentro del rango, ambos extremos incluidos. La emisión de
	/// documentación de asistencia necesita el período completo, no una fecha puntual.
	/// </summary>
	Task<IReadOnlyCollection<PlanillaAsistencia>> BuscarPorDivisionYRangoAsync(Guid divisionID, DateTime desde, DateTime hasta);

	/// <summary>
	/// Planillas de todas las divisiones dentro del rango, ambos extremos incluidos, para los
	/// reportes que recorren la institución entera.
	/// </summary>
	Task<IReadOnlyCollection<PlanillaAsistencia>> BuscarPorRangoAsync(DateTime desde, DateTime hasta);

	/// <summary>
	/// Planillas que tienen registro de alguno de esos cursantes, de cualquier división, fecha y
	/// ciclo lectivo, abiertas y cerradas.
	/// </summary>
	/// <remarks>
	/// La consume el borrado de un alumno, y la consume dos veces: primero para <strong>contar</strong>
	/// qué se va a destruir —el diálogo de confirmación informa cuántos registros de asistencia se
	/// pierden— y después para <strong>borrarlos</strong>. Es la misma consulta en los dos pasos a
	/// propósito: dos consultas distintas podrían dar números distintos y el diálogo diría una cifra
	/// mientras el borrado hace otra.
	///
	/// Sin filtro de fecha ni de división: <c>FK_REGISTRO_ASISTENCIA_CURSANTE</c> es <c>Restrict</c>,
	/// así que un solo registro olvidado en cualquier planilla de cualquier año alcanza para que el
	/// DELETE del cursante falle en la base.
	/// </remarks>
	Task<IReadOnlyCollection<PlanillaAsistencia>> BuscarPorCursantesAsync(IEnumerable<Guid> cursantesIDs);
}
