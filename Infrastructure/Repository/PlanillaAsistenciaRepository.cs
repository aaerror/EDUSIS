using Domain.Asistencias;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class PlanillaAsistenciaRepository : Repository<PlanillaAsistencia>, IPlanillaAsistenciaRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;


	public PlanillaAsistenciaRepository(EdusisDBContext context)
		: base(context) { }

	protected override IQueryable<PlanillaAsistencia> Consulta() =>
		_context.PlanillasAsistencia.Include(x => x.Registros);

	public async Task<bool> ExistePlanillaAsync(Guid divisionID, DateTime fecha) =>
		await _context.PlanillasAsistencia.AnyAsync(x => x.DivisionID == divisionID && x.Fecha == fecha.Date);

	public async Task<PlanillaAsistencia?> BuscarPorDivisionYFechaAsync(Guid divisionID, DateTime fecha) =>
		await Consulta()
			.FirstOrDefaultAsync(x => x.DivisionID == divisionID && x.Fecha == fecha.Date);

	public async Task<int> ContarFaltasAsync(Guid cursanteID, TipoAsistencia tipo) =>
		await _context.PlanillasAsistencia
			.SelectMany(p => p.Registros)
			.CountAsync(r => r.CursanteID == cursanteID && r.Tipo == tipo);
}
