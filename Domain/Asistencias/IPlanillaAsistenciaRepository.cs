using Domain.Shared;

namespace Domain.Asistencias;

public interface IPlanillaAsistenciaRepository : IRepository<PlanillaAsistencia>
{
	Task<bool> ExistePlanillaAsync(Guid divisionID, DateTime fecha);

	Task<PlanillaAsistencia?> BuscarPorDivisionYFechaAsync(Guid divisionID, DateTime fecha);

	Task<int> ContarFaltasAsync(Guid cursanteID, TipoAsistencia tipo);
}
