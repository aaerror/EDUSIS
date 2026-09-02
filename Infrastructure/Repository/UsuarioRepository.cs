using Domain.Usuarios;

namespace Infrastructure.Repository;

public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
	private EdusisDBContext _context => Context as EdusisDBContext;


	public UsuarioRepository(EdusisDBContext context)
		: base(context) { }

	public Usuario BuscarPorEmail(string unEmail) =>
		_context.Usuarios.Where(x => string.Equals(x.Username.ToLower(), unEmail.ToLower())).FirstOrDefault();

	public bool ExisteUsuarioDelDocente(Guid docenteID) =>
		_context.Usuarios.Any(x => x.DocenteID.Equals(docenteID));

	public bool EsEmailInvalido(string unEmail) =>
		_context.Usuarios.Any(x => x.Username.Equals(unEmail));

	public void RecuperarDatosAcceso(string usuario, out string salt, out string hash)
	{
		var usuarioDocente = _context.Usuarios.Find(usuario);
		if (usuarioDocente is null)
		{
			throw new ArgumentException("Usuario docente no registrado en el sistema.", nameof(usuario));
		}

		salt = usuarioDocente.PasswordSalt;
		hash = usuarioDocente.PasswordHash;
	}
}