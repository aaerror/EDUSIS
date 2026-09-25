using Domain.Usuarios;

namespace EDUSIS.TestSupport.Fakes;

/// <summary>Repo fake de <see cref="Usuario"/> (<see cref="IUsuarioRepository"/>) sobre <c>List&lt;Usuario&gt;</c>.</summary>
public sealed class UsuarioRepositorioFake : RepositorioEnMemoria<Usuario>, IUsuarioRepository
{
	/// <summary>Usernames que la prueba fuerza como inválidos, además de los ya presentes.</summary>
	public HashSet<string> UsernamesInvalidos { get; } = new();

	public Task<Usuario?> BuscarPorUsernameAsync(string username) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.Username.ToLower() == username.Trim().ToLower()));

	public Task<Usuario?> BuscarPorDocenteAsync(Guid docenteID) =>
		Task.FromResult(_entidades.FirstOrDefault(x => x.DocenteID.Equals(docenteID)));

	public Task<bool> ExisteUsuarioDelDocenteAsync(Guid docenteID) =>
		Task.FromResult(_entidades.Any(x => x.DocenteID.Equals(docenteID)));

	public Task<bool> EsUsernameInvalidoAsync(string username) =>
		Task.FromResult(UsernamesInvalidos.Contains(username) || _entidades.Any(x => x.Username.ToLower() == username.Trim().ToLower()));
}
