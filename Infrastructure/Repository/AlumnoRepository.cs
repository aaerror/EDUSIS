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

	/// <remarks>
	/// Busca en las dos columnas por separado y NO sobre <c>Apellido + ", " + Nombre</c>, que es lo
	/// que hacía la versión exacta: concatenar obliga a acertar el separador, y un término como
	/// "juan" no coincide con "Pérez, Juan" por el prefijo del apellido que tiene delante.
	///
	/// Sin <c>ToLower()</c> sobre la columna, a diferencia de <c>DocenteRepository</c>: la base usa
	/// una colación insensible a mayúsculas, así que el <c>LOWER()</c> no cambia qué filas vuelven
	/// —sólo impide que SQL Server use el índice del apellido—. El <c>Trim</c> del término sí queda,
	/// porque el espacio sobrante lo aporta quien tipea.
	/// </remarks>
	public async Task<IReadOnlyCollection<Alumno>> BuscarSegunNombreCompletoAsync(string termino)
	{
		var patron = $"%{ termino.Trim() }%";

		return await Consulta()
			.Where(x => EF.Functions.Like(x.DatosPersonales.Apellido, patron) ||
						EF.Functions.Like(x.DatosPersonales.Nombre, patron))
			.OrderBy(x => x.DatosPersonales.Apellido)
			.ThenBy(x => x.DatosPersonales.Nombre)
			.ToListAsync();
	}

	public async Task<Alumno?> BuscarPorDocumentoAsync(string documento) =>
		await Consulta()
			.FirstOrDefaultAsync(x => EF.Functions.Like(x.DatosPersonales.Documento, documento));
}
