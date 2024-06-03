using Core.ServicioAutenticaciones.DTOs.Request;
using Core.ServicioAutenticaciones.DTOs.Response;
using Core.Shared;

namespace Core.ServicioAutenticaciones;

public interface IServicioAutenticacion : IServicio
{
    void RegistrarUsuario(RegistrarUsuarioRequest request);

    UsuarioResponse Login(LoginRequest request);
}
