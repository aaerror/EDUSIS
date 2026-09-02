using Core.ServicioAutenticaciones.DTOs.Requests;
using Core.ServicioAutenticaciones.DTOs.Responses;

namespace Core.ServicioAutenticaciones;

public interface IServicioAutenticacion
{
	Task<UsuarioResponse> Login(LoginRequest request);

	void Logout(LogoutRequest request);
}