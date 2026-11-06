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

	public async Task<IReadOnlyCollection<PlanillaAsistencia>> BuscarPorDivisionYRangoAsync(Guid divisionID, DateTime desde, DateTime hasta) =>
		await Consulta()
			.Where(x => x.DivisionID == divisionID && x.Fecha >= desde.Date && x.Fecha <= hasta.Date)
			.OrderBy(x => x.Fecha)
			.ToListAsync();

	public async Task<IReadOnlyCollection<PlanillaAsistencia>> BuscarPorDivisionAsync(Guid divisionID) =>
		await Consulta()
			.Where(x => x.DivisionID == divisionID)
			.OrderBy(x => x.Fecha)
			.ToListAsync();

	public async Task<IReadOnlyCollection<PlanillaAsistencia>> BuscarPorRangoAsync(DateTime desde, DateTime hasta) =>
		await Consulta()
			.Where(x => x.Fecha >= desde.Date && x.Fecha <= hasta.Date)
			.OrderBy(x => x.Fecha)
			.ToListAsync();

	/// <remarks>
	/// Materializa los identificadores antes del <c>Where</c>: <c>Contains</c> sobre un
	/// <c>IEnumerable</c> diferido se traduce a SQL de forma impredecible, y acá la colección viene
	/// de otra consulta ya ejecutada. Con la lista en memoria, EF emite un <c>IN (…)</c>.
	///
	/// Devuelve vacío sin ir a la base cuando no hay cursantes: es el caso corriente —un alumno sin
	/// una sola inscripción— y un <c>IN ()</c> vacío no tiene traducción útil.
	/// </remarks>
	public async Task<IReadOnlyCollection<PlanillaAsistencia>> BuscarPorCursantesAsync(IEnumerable<Guid> cursantesIDs)
	{
		var ids = cursantesIDs.Distinct().ToList();
		if (ids.Count is 0)
		{
			return Array.Empty<PlanillaAsistencia>();
		}

		return await Consulta()
			.Where(x => x.Registros.Any(r => ids.Contains(r.CursanteID)))
			.OrderBy(x => x.Fecha)
			.ToListAsync();
	}
}
