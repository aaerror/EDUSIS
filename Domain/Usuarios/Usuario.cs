using Domain.Shared;
using Domain.Usuarios.Exceptions;

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
			throw new UsuarioSinDocenteException();
		}

		if (string.IsNullOrWhiteSpace(username))
		{
			throw new NombreUsuarioRequeridoException();
		}

		if (passwordSalt is null || passwordHash is null)
		{
			throw new ArgumentNullException(passwordSalt is null ? nameof(passwordSalt) : nameof(passwordHash), "Error en la contraseña.");
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
	/// Por <c>Id</c> y no por descripción: el Id es canónico y la descripción es presentación, vive en
	/// una columna <c>varchar</c> no unicode y tres de los seis roles llevan acento
	/// ("Dirección", "Preceptoría", "Secretaría Académica"). Un literal mal escrito por la sobrecarga
	/// de <c>string</c> falla en ejecución; acá no hay literal que escribir mal.
	/// </remarks>
	public void AgregarRol(int rolID) =>
		AgregarRol(Rol.DesdeId(rolID));

	/// <summary>Quita el rol por su <see cref="Enumeration.Id"/>. Ver <see cref="AgregarRol(int)"/>.</summary>
	public void QuitarRol(int rolID) =>
		QuitarRol(Rol.DesdeId(rolID));

	/// <remarks>
	/// Resuelve el rol por <strong>descripción</strong>. Se conserva porque la usan
	/// <c>Tools/EDUSIS.Seed</c> y los builders de prueba, donde el literal está a la vista del que lo
	/// escribe; para los casos de uso está <see cref="AgregarRol(int)"/>, que no depende del texto.
	/// </remarks>
	public void AgregarRol(string unRol) =>
		AgregarRol(Rol.DesdeDescripcion(unRol));

	/// <remarks>Ver <see cref="AgregarRol(string)"/>: resuelve por descripción.</remarks>
	public void QuitarRol(string unRol) =>
		QuitarRol(Rol.DesdeDescripcion(unRol));

	private void AgregarRol(Rol rol)
	{
		if (_roles.Contains(rol))
		{
			throw new RolYaAsignadoException();
		}

		_roles.Add(rol);
	}

	private void QuitarRol(Rol rol)
	{
		if (!_roles.Contains(rol))
		{
			throw new RolNoAsignadoException();
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