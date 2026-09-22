using Domain.Usuarios;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Usuario"/>. Estado por defecto válido: docente asociado arbitrario,
/// nombre de usuario, salt y hash de prueba (no son credenciales reales). <see cref="ConRol"/>
/// encola roles que <see cref="Build"/> agrega vía <c>Usuario.AgregarRol</c> — que resuelve el
/// rol por su descripción (<c>Administrador</c>, <c>Dirección</c>, <c>Docente</c>, <c>Secretaria</c>).
/// </summary>
public sealed class UsuarioBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _docenteID = Guid.NewGuid();
	private string _username = "maria.gonzalez";
	private string _passwordSalt = "c2FsLWRlLXBydWViYQ==";
	private string _passwordHash = "aGFzaC1kZS1wcnVlYmE=";
	private readonly List<string> _roles = new();
	#endregion

	#region CONFIGURACIÓN
	public UsuarioBuilder ConDocente(Guid docenteID)
	{
		_docenteID = docenteID;
		return this;
	}

	public UsuarioBuilder ConUsername(string username)
	{
		_username = username;
		return this;
	}

	public UsuarioBuilder ConPasswordSalt(string passwordSalt)
	{
		_passwordSalt = passwordSalt;
		return this;
	}

	public UsuarioBuilder ConPasswordHash(string passwordHash)
	{
		_passwordHash = passwordHash;
		return this;
	}

	public UsuarioBuilder ConRol(string rol)
	{
		_roles.Add(rol);
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Usuario Build()
	{
		var usuario = new Usuario(_docenteID, _username, _passwordSalt, _passwordHash);

		foreach (var rol in _roles)
		{
			usuario.AgregarRol(rol);
		}

		return usuario;
	}
	#endregion
}
