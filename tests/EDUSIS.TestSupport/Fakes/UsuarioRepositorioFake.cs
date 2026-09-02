using Domain.Usuarios;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Usuario"/> (<see cref="IUsuarioRepository"/>) sobre <c>List&lt;Usuario&gt;</c>.</summary>
public sealed class UsuarioRepositorioFake : RepositorioEnMemoria<Usuario>, IUsuarioRepository
{
	/// <summary>Emails/usernames que la prueba fuerza como inválidos, además de los ya presentes.</summary>
	public HashSet<string> EmailsInvalidos { get; } = new();

	public Usuario BuscarPorEmail(string unUsuario) =>
		_entidades.FirstOrDefault(x => x.Username == unUsuario)!;

	public bool ExisteUsuarioDelDocente(Guid docenteID) =>
		_entidades.Any(x => x.DocenteID.Equals(docenteID));

	public bool EsEmailInvalido(string usuario) =>
		EmailsInvalidos.Contains(usuario) || _entidades.Any(x => x.Username == usuario);

	public void RecuperarDatosAcceso(string usuario, out string salt, out string hash)
	{
		var encontrado = _entidades.FirstOrDefault(x => x.Username == usuario);
		salt = encontrado?.PasswordSalt ?? string.Empty;
		hash = encontrado?.PasswordHash ?? string.Empty;
	}
}
