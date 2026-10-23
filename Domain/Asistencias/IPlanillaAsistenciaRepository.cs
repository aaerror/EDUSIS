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
}
