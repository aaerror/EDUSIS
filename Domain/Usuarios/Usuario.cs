using Domain.Shared;

namespace Domain.Usuarios;

public class Usuario : Entity
{
	private List<Rol> _roles = new();

	public Guid DocenteID { get; private set; } = Guid.Empty;
	public string Username { get; private set; }
	public string PasswordSalt { get; private set; }
	public string PasswordHash { get; private set; }
	public IReadOnlyCollection<Rol> Roles => _roles.AsReadOnly();


	#region CONSTRUCTOR
	private Usuario() { }

	private Usuario(Guid usuarioID)
		: base(usuarioID) { }

	private Usuario(Guid usuarioID, Guid docenteID, string username, string passwordSalt, string passwordHash)
		: this(usuarioID)
	{
		if (Guid.Empty.Equals(docenteID))
		{
			throw new ArgumentNullException(nameof(docenteID), "Se debe asignar un docente.");
		}

		if (string.IsNullOrWhiteSpace(username))
		{
			throw new ArgumentNullException(nameof(username), "Se debe asignar un nombre de usuario.");
		}

		if (passwordSalt is null || passwordHash is null)
		{
			throw new ArgumentNullException("Error en la contraseña.");
		}

		/*if (!Regex.IsMatch(username, @"^[a-zA-Z0-9](_(?!(\.|_))|\.(?!(_|\.))|[a-zA-Z0-9]){6,18}[a-zA-Z0-9]$", RegexOptions.None))
		{
			throw new ArgumentException("El nombre de usuario no es válido", nameof(username));
		}*/

		DocenteID = docenteID;
		Username = username;
		PasswordSalt = passwordSalt;
		PasswordHash = passwordHash;
	}

	private Usuario(Guid usuarioID, Guid docenteID, string username, string passwordSalt, string passwordHash, List<Rol> roles)
		: this(usuarioID, docenteID, username, passwordSalt, passwordHash)
	{
		_roles = roles;
	}

	public Usuario(Guid docenteID, string username, string passwordSalt, string passwordHash)
		: this(Guid.NewGuid(), docenteID, username, passwordSalt, passwordHash) { }

	public Usuario(Guid docenteID, string username, string passwordSalt, string passwordHash, List<Rol> roles)
		: this(Guid.NewGuid(), docenteID, username, passwordSalt, passwordHash, roles) { }
	#endregion

	public void AgregarRol(string unRol)
	{
		var rol = Enumeration.FromDescripcion<Rol>(unRol);
		bool existe = _roles.Contains(rol);
		if (existe)
		{
			throw new ArgumentException("El rol ya se encuentra asignado al usuario.", nameof(unRol));
		}

		_roles.Add(rol);
	}

	public void QuitarRol(string unRol)
	{
		var rol = Enumeration.FromDescripcion<Rol>(unRol);
		var existeRol = _roles.Contains(rol);
		if (!existeRol)
		{
			throw new ArgumentException("El rol no se encuentra asignado al usuario.", nameof(unRol));
		}

		_roles.Remove(rol);
	}

	public void CambiarPassword(string passwordSalt, string passwordHash)
	{
		PasswordSalt = passwordSalt;
		PasswordHash = passwordHash;
	}
}