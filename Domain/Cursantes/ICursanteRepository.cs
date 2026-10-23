using Domain.Shared;

namespace Domain.Cursantes;

public interface ICursanteRepository : IRepository<Cursante>
{
	Task<bool> ExisteInscripcionAsync(Guid alumnoID, CicloLectivo cicloLectivo);
	Task<int> ContarCursantesDeDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo);
	Task<Cursante?> BuscarInscripcionActivaAsync(Guid alumnoID, CicloLectivo cicloLectivo);
	Task<IReadOnlyCollection<Cursante>> BuscarPorDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo);
	Task<IReadOnlyCollection<Guid>> BuscarInscriptosEnFechaAsync(Guid divisionID, DateTime fecha);

	/// <summary>
	/// Todas las inscripciones del ciclo lectivo, sin filtrar por división. Es la base de los
	/// reportes institucionales, que recorren la matrícula completa y agrupan después.
	/// </summary>
	Task<IReadOnlyCollection<Cursante>> BuscarPorCicloLectivoAsync(CicloLectivo cicloLectivo);
}
