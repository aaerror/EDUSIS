using Core.ServicioAutenticaciones.DTOs.Requests;
using Core.ServicioAutenticaciones.DTOs.Responses;
using Core.ServicioSecurity;
using Core.ServicioUsuarios.DTOs.Requests;
using Core.ServicioUsuarios;
using Core.Shared;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioAutenticaciones;

internal class ServicioAutenticacion : IServicio, IServicioAutenticacion
{
	private readonly ILogger _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IServicioSeguridad _servicioSeguridad;
	private readonly IServicioUsuario _servicioUsuario;


	public ServicioAutenticacion(ILogger<ServicioAutenticacion> logger, IUnitOfWork unitOfWork, IServicioSeguridad servicioSeguridad, IServicioUsuario servicioUsuario)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
		_servicioSeguridad = servicioSeguridad;
		_servicioUsuario = servicioUsuario;
	}

	public async Task<UsuarioResponse> Login(LoginRequest request)
	{
		string salt = string.Empty;
		string hash = string.Empty;

		if (request is null)
		{
			throw new NullReferenceException("Datos incompletos para acceder al sistema.");
		}

		try
		{
			_logger.LogInformation($"Realizando ingreso al sistema del usuario...");

			var usuario = _unitOfWork.Usuarios.BuscarPorEmail(request.credential.UserName);
			if (usuario is null)
			{
				throw new ArgumentException("Datos de acceso incorrectos.");
			}

			//_unitOfWork.Usuarios.RecuperarDatosAcceso(request.Email, out salt, out hash);
			var esAccesoValido = _servicioSeguridad.ValidatePassword(request.credential.SecurePassword, usuario.PasswordSalt, usuario.PasswordHash);
			if (!esAccesoValido)
			{
				throw new ArgumentException("Datos de acceso incorrectos.");
			}

			var response = await _servicioUsuario.BuscarUsuarioPorIDAsync(new UsuarioIDRequest(usuario.Id));

			return response;
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public void Logout(LogoutRequest request)
	{
		throw new NotImplementedException();
	}
}