using Domain.Alumnos;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class AlumnoRepository : PersonaRepository<Alumno>, IAlumnoRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;

	public AlumnoRepository(EdusisDBContext context)
		: base(context) { }

	public async Task<bool> EsLegajoInvalidoAsync(string legajo) =>
		await _context.Alumnos.AnyAsync(x => EF.Functions.Like(x.Legajo, legajo));

	// Proyección del legajo: el agregado hidratado no hace falta para derivar el siguiente (CORE-13).
	public async Task<string?> BuscarUltimoLegajoAsync() =>
		await _context.Alumnos
			.Select(x => x.Legajo)
			.OrderByDescending(x => x)
			.FirstOrDefaultAsync();

	// Replica DatosPersonales.NombreCompleto() ("Apellido, Nombre") sobre columnas mapeadas:
	// el método del dominio no es traducible a SQL (H-020).
	public async Task<Alumno?> BuscarPorNombreCompletoAsync(string nombreCompleto) =>
		await Consulta()
			.FirstOrDefaultAsync(x => EF.Functions.Like(
				x.DatosPersonales.Apellido + ", " + x.DatosPersonales.Nombre,
				nombreCompleto.Trim()));

	public async Task<Alumno?> BuscarPorDocumentoAsync(string documento) =>
		await Consulta()
			.FirstOrDefaultAsync(x => EF.Functions.Like(x.DatosPersonales.Documento, documento));
}
