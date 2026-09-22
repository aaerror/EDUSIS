using Domain.Shared;

namespace Domain.Cursantes;

public interface ICursanteRepository : IRepository<Cursante>
{
	Task<bool> ExisteInscripcionAsync(Guid alumnoID, CicloLectivo cicloLectivo);
	Task<int> ContarCursantesDeDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo);
	Task<Cursante?> BuscarInscripcionActivaAsync(Guid alumnoID, CicloLectivo cicloLectivo);
	Task<IReadOnlyCollection<Cursante>> BuscarPorDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo);
	Task<IReadOnlyCollection<Guid>> BuscarInscriptosEnFechaAsync(Guid divisionID, DateTime fecha);
}
