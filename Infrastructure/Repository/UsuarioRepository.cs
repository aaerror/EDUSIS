using Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

internal class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
	private EdusisDBContext _context => (EdusisDBContext)Context;

	public UsuarioRepository(EdusisDBContext context)
		: base(context) { }

	protected override IQueryable<Usuario> Consulta() =>
		_context.Usuarios.Include(x => x.Roles);

	private static string NormalizarUsername(string username) =>
		username.Trim().ToLower();

	public async Task<Usuario?> BuscarPorUsernameAsync(string username)
	{
		var normalizado = NormalizarUsername(username);
		return await Consulta().FirstOrDefaultAsync(x => x.Username.ToLower() == normalizado);
	}

	public async Task<Usuario?> BuscarPorDocenteAsync(Guid docenteID) =>
		await Consulta().FirstOrDefaultAsync(x => x.DocenteID == docenteID);

	public async Task<bool> ExisteUsuarioDelDocenteAsync(Guid docenteID) =>
		await _context.Usuarios.AnyAsync(x => x.DocenteID == docenteID);

	public async Task<bool> EsUsernameInvalidoAsync(string username)
	{
		var normalizado = NormalizarUsername(username);
		return await _context.Usuarios.AnyAsync(x => x.Username.ToLower() == normalizado);
	}
}