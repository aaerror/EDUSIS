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

	/// <summary>
	/// Asigna el rol por su <see cref="Enumeration.Id"/>. <strong>Es la sobrecarga que deben usar los
	/// casos de uso y la interfaz.</strong>
	/// </summary>
	/// <remarks>
	/// Por <c>Id</c> y no por descripción, que es la decisión #6 de
	/// <c>docs/plan/rbac-y-seed.md</c>: el Id es canónico y la descripción es presentación, vive en
	/// una columna <c>varchar</c> no unicode y tres de los seis roles llevan acento
	/// ("Dirección", "Preceptoría", "Secretaría Académica"). Un literal mal escrito por la sobrecarga
	/// de <c>string</c> falla en ejecución; acá no hay literal que escribir mal.
	/// </remarks>
	public void AgregarRol(int rolID) =>
		AgregarRol(Enumeration.FromValue<Rol>(rolID));

	/// <summary>Quita el rol por su <see cref="Enumeration.Id"/>. Ver <see cref="AgregarRol(int)"/>.</summary>
	public void QuitarRol(int rolID) =>
		QuitarRol(Enumeration.FromValue<Rol>(rolID));

	/// <remarks>
	/// Resuelve el rol por <strong>descripción</strong>. Se conserva porque la usan
	/// <c>Tools/EDUSIS.Seed</c> y los builders de prueba, donde el literal está a la vista del que lo
	/// escribe; para los casos de uso está <see cref="AgregarRol(int)"/>, que no depende del texto.
	/// </remarks>
	public void AgregarRol(string unRol) =>
		AgregarRol(Enumeration.FromDescripcion<Rol>(unRol));

	/// <remarks>Ver <see cref="AgregarRol(string)"/>: resuelve por descripción.</remarks>
	public void QuitarRol(string unRol) =>
		QuitarRol(Enumeration.FromDescripcion<Rol>(unRol));

	private void AgregarRol(Rol rol)
	{
		if (_roles.Contains(rol))
		{
			throw new ArgumentException("El rol ya se encuentra asignado al usuario.", nameof(rol));
		}

		_roles.Add(rol);
	}

	private void QuitarRol(Rol rol)
	{
		if (!_roles.Contains(rol))
		{
			throw new ArgumentException("El rol no se encuentra asignado al usuario.", nameof(rol));
		}

		_roles.Remove(rol);
	}

	/// <summary>
	/// Autorización del usuario: ¿alguno de sus roles habilita esta acción? Sin roles no hay
	/// permisos (se niega por omisión), y la pregunta es por permiso y no por rol para que
	/// mover una atribución sea un cambio en <see cref="Rol"/> y no en cada punto de uso.
	/// </summary>
	public bool TienePermiso(Permiso unPermiso) =>
		_roles.Any(rol => rol.Tiene(unPermiso));

	public void CambiarPassword(string passwordSalt, string passwordHash)
	{
		PasswordSalt = passwordSalt;
		PasswordHash = passwordHash;
	}
}