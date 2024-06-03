using Domain.Usuarios;

namespace Core.ServicioAutenticaciones.DTOs.Response;

public record UsuarioResponse
{
    public Guid DocenteID { get; init; }
    public Rol Rol { get; init; }
    public IReadOnlyCollection<Acceso> Accesos { get; init; }


    public UsuarioResponse(Guid docenteID, Rol rol, IReadOnlyCollection<Acceso> accesos)
    {
        DocenteID = docenteID;
        Rol = rol;
        Accesos = accesos;
    }
}
