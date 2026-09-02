using Domain.Alumnos;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class AlumnoRepository : Repository<Alumno>, IAlumnoRepository
{
	private EdusisDBContext _context => Context as EdusisDBContext;


	public AlumnoRepository(EdusisDBContext context)
		: base(context) { }

	public async Task<bool> EsDocumentoInvalidoAsync(string documento) =>
		await _context.Alumnos.AnyAsync(x => EF.Functions.Like(x.DatosPersonales.Documento, documento));
		//await _context.Alumnos.AnyAsync(x => string.Equals(x.DatosPersonales.Documento, documento, StringComparison.InvariantCultureIgnoreCase));

	public async Task<bool> ExisteIDAsync(Guid id) =>
		await _context.Alumnos.AnyAsync(x => x.Id == id);

	public bool EsLegajoValido(string legajo) =>
		throw new NotImplementedException();

	public async Task<Alumno?> BuscarPorNombreCompletoAsync(string nombreCompleto) =>
		await _context.Alumnos
			.FirstOrDefaultAsync(x => EF.Functions.Like(x.DatosPersonales.NombreCompleto(), nombreCompleto));

	public async Task<Alumno?> BuscarPorDocumentoAsync(string documento) =>
		await _context.Alumnos
			.FirstOrDefaultAsync(x => EF.Functions.Like(x.DatosPersonales.Documento, documento));
}
