using Domain.Cursantes;
using Domain.Shared;
using Infrastructure.EntityConfigurations;
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

	// Misma condición que ContarInscripcionesDeDivisionAsync, pero por Consulta(), que incluye las
	// calificaciones: el borrado las necesita cargadas para informar cuántas destruye, y el
	// Include es lo que hace que EF las traiga en la misma ida a la base en lugar de una por
	// cursante.
	public async Task<IReadOnlyCollection<Cursante>> BuscarInscripcionesDeDivisionAsync(Guid divisionID) =>
		await Consulta()
			.Where(x => x.DivisionID == divisionID)
			.OrderBy(x => x.FechaInicio)
			.ToListAsync();

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

	// El catálogo de ciclos lectivos no es una entidad de dominio: es la tabla ciclo_lectivo
	// mapeada como shared-type entity sobre Dictionary<string, object> (ver
	// CicloLectivoConfiguration), por eso se llega a ella con Set<...>(nombre) y se lee la columna
	// por EF.Property en vez de por una propiedad CLR.
	//
	// El orden descendente sale numérico y no alfabético porque "periodo" tiene conversión a
	// smallint: EF ordena por la columna del proveedor, no por el string.
	public async Task<IReadOnlyCollection<CicloLectivo>> ListarCiclosLectivosAsync()
	{
		var periodos = await _context.Set<Dictionary<string, object>>(CicloLectivoConfiguration.CATALOGO)
			.OrderByDescending(x => EF.Property<string>(x, "periodo"))
			.Select(x => EF.Property<string>(x, "periodo"))
			.ToListAsync();

		return periodos.Select(CicloLectivo.Crear).ToList();
	}
}
