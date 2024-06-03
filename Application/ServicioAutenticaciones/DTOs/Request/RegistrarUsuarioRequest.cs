using Domain.Usuarios;

namespace Core.ServicioAutenticaciones.DTOs.Request;

public record RegistrarUsuarioRequest
{
    public Guid DocenteID { get; init; }
    public string Usuario { get; init; }
    public string Clave { get; init; }
    public Rol Rol { get; init; }

    public RegistrarUsuarioRequest(Guid docenteID, string usuario, string clave, Rol rol)
    {
        DocenteID = docenteID;
        Usuario = usuario;
        Clave = clave;
        Rol = rol;
    }
}
