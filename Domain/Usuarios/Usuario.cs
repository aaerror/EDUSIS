using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Usuarios;

public class Usuario : Entity
{
    private List<Acceso> _accesos = new List<Acceso>();

    public Guid DocenteID { get; private set; } = Guid.Empty;
    public Rol Rol { get; private set; }
    public string Username { get; private set; }
    public string PasswordSalt { get; private set; }
    public string PasswordHash { get; private set; }
    public IReadOnlyCollection<Acceso> Accesos => _accesos.AsReadOnly();


    protected Usuario(Guid usuarioID) : base(usuarioID) { }

    protected Usuario(Guid usuarioID, Guid docenteID, string username, string passwordSalt, string passwordHash, Rol rol)
        : this(usuarioID)
    {
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9](_(?!(\.|_))|\.(?!(_|\.))|[a-zA-Z0-9]){6,18}[a-zA-Z0-9]$", RegexOptions.None))
        {
            throw new ArgumentException(nameof(username), "El nombre de usuario no es válido");
        }

        DocenteID = docenteID;
        Username = username;
        PasswordSalt = passwordSalt;
        PasswordHash = passwordHash;

        if (Rol.Administrador.Equals(rol))
        {
            _accesos.Add(Acceso.Crear(Permiso.Escribir));
            _accesos.Add(Acceso.Crear(Permiso.Ejecutar));
            _accesos.Add(Acceso.Crear(Permiso.Leer));
        }
    }

    public Usuario(Guid docenteID, string username, string passwordSalt, string passwordHash, Rol rol)
        : this(Guid.NewGuid(), docenteID, username, passwordSalt, passwordHash, rol)
    {
        if (Guid.Empty.Equals(docenteID))
        {
            throw new ArgumentNullException(nameof(docenteID), "Se debe asignar un docente");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentNullException(nameof(username), "Se debe asignar un nombre de usuario");
        }

        if (passwordSalt is null || passwordHash is null)
        {
            throw new ArgumentNullException("Error en la contraseña.");
        }
    }

    public void AgregarPermiso(Permiso unPermiso)
    {
        bool existe = _accesos.Any(x => x.Permiso.Equals(unPermiso) && x.PermisoHabilitado());
        if (existe)
        {
            throw new ArgumentException("El permiso ya se encuentra asignado al usuario.", nameof(unPermiso));
        }

        var nuevoAcceso = Acceso.Crear(unPermiso);
        _accesos.Add(nuevoAcceso);
    }

    public void QuitarPermiso(Permiso unPermiso)
    {
        var acceso = _accesos.Find(x => x.Permiso.Equals(unPermiso) && x.PermisoHabilitado());
        if (acceso is null)
        {
            throw new ArgumentException("El permiso no se encuentra asignado al usuario.", nameof(unPermiso));
        }

        int index = _accesos.IndexOf(acceso);
        _accesos[index] = acceso.AnularPermiso();
    }

    public void CambiarPassword(string passwordSalt, string passwordHash)
    {
        // TODO: Crear metodo cambiar password
    }
}
