using Core.ServicioAutenticaciones.DTOs.Responses;
using Core.ServicioUsuarios.DTOs.Requests;

namespace Core.ServicioUsuarios;

public interface IServicioUsuario
{
	Task<UsuarioResponse> BuscarUsuarioPorIDAsync(UsuarioIDRequest request);
	void ActualizarRol(ActualizarRolRequest request);
	Task RegistrarUsuarioAsync(RegistrarUsuarioRequest request);
	Task RestablecerAccesoAsync(RestablecerAccesoRequest request);
	Task SolicitarAccesoAsync(SolicitarAccesoRequest request);
}