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

	public async Task<bool> ExisteInscripcionActivaAsync(Guid alumnoID, CicloLectivo cicloLectivo) =>
		await _context.Cursantes.AnyAsync(x => x.AlumnoID == alumnoID && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null);

	public async Task<int> ContarCursantesDeDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo) =>
		await _context.Cursantes.CountAsync(x => x.DivisionID == divisionID && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null);

	// Todas las inscripciones de la división, sin filtrar por ciclo lectivo ni por FechaFin: la
	// clave ajena FK_CURSANTE_DIVISION es Restrict y una inscripción cerrada bloquea el DELETE
	// igual que una activa.
	public async Task<int> ContarInscripcionesDeDivisionAsync(Guid divisionID) =>
		await _context.Cursantes.CountAsync(x => x.DivisionID == divisionID);

	public async Task<Cursante?> BuscarInscripcionActivaAsync(Guid alumnoID, CicloLectivo cicloLectivo) =>
		await Consulta()
			.FirstOrDefaultAsync(x => x.AlumnoID == alumnoID && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null);

	// Matrícula vigente de la división: el filtro por FechaFin deja afuera al alumno trasladado,
	// que ya figura en la división de destino con una inscripción nueva.
	public async Task<IReadOnlyCollection<Cursante>> BuscarPorDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo) =>
		await Consulta()
			.Where(x => x.DivisionID == divisionID && x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null)
			.ToListAsync();

	// Matrícula vigente del ciclo lectivo, que alimenta los cuatro reportes institucionales: sin el
	// filtro por FechaFin el alumno trasladado se cuenta una vez por cada inscripción.
	public async Task<IReadOnlyCollection<Cursante>> BuscarPorCicloLectivoAsync(CicloLectivo cicloLectivo) =>
		await Consulta()
			.Where(x => x.CicloLectivo.Periodo == cicloLectivo.Periodo && x.FechaFin == null)
			.ToListAsync();

	public async Task<IReadOnlyCollection<Cursante>> BuscarInscripcionesDelAlumnoAsync(Guid alumnoID) =>
		await Consulta()
			.Where(x => x.AlumnoID == alumnoID)
			.OrderBy(x => x.FechaInicio)
			.ToListAsync();

	public async Task<IReadOnlyCollection<Cursante>> BuscarInscripcionesDelCicloAsync(Guid alumnoID, CicloLectivo cicloLectivo) =>
		await Consulta()
			.Where(x => x.AlumnoID == alumnoID && x.CicloLectivo.Periodo == cicloLectivo.Periodo)
			.OrderBy(x => x.FechaInicio)
			.ToListAsync();

	public async Task<IReadOnlyCollection<Guid>> BuscarInscriptosEnFechaAsync(Guid divisionID, DateTime fecha) =>
		await _context.Cursantes
			.Where(x => x.DivisionID == divisionID && x.FechaInicio.Date <= fecha.Date && (x.FechaFin == null || x.FechaFin.Value.Date >= fecha.Date))
			.Select(x => x.Id)
			.ToListAsync();
}
