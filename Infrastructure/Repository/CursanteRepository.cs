using Domain.Cursantes;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class CursanteRepository : Repository<Cursante>, ICursanteRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;


	public CursanteRepository(EdusisDBContext context)
		: base(context) { }

	protected override IQueryable<Cursante> Consulta() =>
		_context.Cursantes.Include(x => x.Calificaciones);

	public async Task<bool> ExisteInscripcionAsync(Guid alumnoID, CicloLectivo cicloLectivo) =>
		await _context.Cursantes.AnyAsync(x => x.AlumnoID == alumnoID && x.CicloLectivo.Periodo == cicloLectivo.Periodo);

	public async Task<int> ContarCursantesDeDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo) =>
		await _context.Cursantes.CountAsync(x => x.DivisionID == divisionID && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null);

	public async Task<Cursante?> BuscarInscripcionActivaAsync(Guid alumnoID, CicloLectivo cicloLectivo) =>
		await Consulta()
			.FirstOrDefaultAsync(x => x.AlumnoID == alumnoID && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null);

	public async Task<IReadOnlyCollection<Cursante>> BuscarPorDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo) =>
		await Consulta()
			.Where(x => x.DivisionID == divisionID && x.CicloLectivo.Periodo == cicloLectivo.Periodo)
			.ToListAsync();

	public async Task<IReadOnlyCollection<Guid>> BuscarInscriptosEnFechaAsync(Guid divisionID, DateTime fecha) =>
		await _context.Cursantes
			.Where(x => x.DivisionID == divisionID && x.FechaInicio.Date <= fecha.Date && (x.FechaFin == null || x.FechaFin.Value.Date >= fecha.Date))
			.Select(x => x.Id)
			.ToListAsync();
}
