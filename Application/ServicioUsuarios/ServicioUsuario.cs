using Core.ServicioAutenticaciones.DTOs.Responses;
using Core.ServicioSecurity;
using Core.ServicioUsuarios.DTOs.Requests;
using Core.Shared;
using Domain.Usuarios;
using Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Core.ServicioUsuarios;

internal class ServicioUsuario : IServicio, IServicioUsuario
{
	private readonly ILogger<ServicioUsuario> _logger;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IServicioSeguridad _servicioSeguridad;


	public ServicioUsuario(ILogger<ServicioUsuario> logger, IUnitOfWork unitOfWork, IServicioSeguridad servicioSeguridad)
	{
		_logger = logger;
		_unitOfWork = unitOfWork;
		_servicioSeguridad = servicioSeguridad;
	}

	public async Task<UsuarioResponse> BuscarUsuarioPorIDAsync(UsuarioIDRequest request)
	{
		try
		{
			var usuario = await _unitOfWork.Usuarios.BuscarPorIDAsync(request.UsuarioID);
			if (usuario is null)
			{
				throw new NullReferenceException($"No se encontró el usuario.");
			}

			var docente = await _unitOfWork.Docentes.BuscarPorIDAsync(usuario.DocenteID);

			return new UsuarioResponse(usuario.Id,
									   docente.Id,
									   usuario.Username,
									   docente.DatosPersonales.NombreCompleto(),
									   docente.Puesto is not null ? docente.Puesto.Posicion.ToString() : "Sin puesto docente asignado",
									   new List<string>()
									   {
										   "Basic",
										   "Custom"
									   });
									   //usuario.Roles.Count() > 0 ? usuario.Roles.Select(x => x.Descripcion).ToList() : new List<string>());
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task RegistrarUsuarioAsync(RegistrarUsuarioRequest request)
	{
		if (request is null)
		{
			throw new NullReferenceException("Datos incompletos para registrar el usuario.");
		}

		try
		{
			_logger.LogInformation($"Generando usuario para el docente {request.DocenteID}...");
			var existeUsuario = _unitOfWork.Usuarios.ExisteUsuarioDelDocente(request.DocenteID);
			if (existeUsuario)
			{
				throw new ArgumentException("El docente ya cuenta con un usuario en el sistema.", nameof(request.DocenteID));
			}

			var docente = await _unitOfWork.Docentes.BuscarPorIDAsync(request.DocenteID);
			var dni = docente.DatosPersonales.Documento;
			var email = docente.Email;

			/*if (_unitOfWork.Usuarios.EsEmailInvalido(request.Email))
			{
				throw new ArgumentException("El nombre de usuario ya se encuentra en uso.", nameof(request.Email));
			}

			if (_unitOfWork.Usuarios.ExisteUsuarioDelDocente(request.DocenteID))
			{
				throw new ArgumentException("El docente ya cuenta con un usuario en el sistema.", nameof(request.DocenteID));
			}*/

			var passwordHashed = _servicioSeguridad.HashPassword(dni);
			var salt = passwordHashed.GetValueOrDefault("salt");
			var hash = passwordHashed.GetValueOrDefault("hash");

			/*char[] delimiter = { ':' };
			string[] split = saltAndHash.Split(delimiter);*/

			var usuario = new Usuario(request.DocenteID, email, salt, hash);

			await _unitOfWork.Usuarios.AgregarAsync(usuario);
			await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation("Usuario creado correctamente...");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task SolicitarAccesoAsync(SolicitarAccesoRequest request)
	{
		try
		{
			var esValido = await _unitOfWork.Docentes.BuscarAsync(x => x.Id.Equals(request.DocenteID) && x.Legajo.Equals(request.Legajo));
			if (!esValido.Any())
			{
				throw new ArgumentException("Datos del docente incorrectos.");
			}

			var docente = await _unitOfWork.Docentes.BuscarPorIDAsync(request.DocenteID);
			var dni = docente.DatosPersonales.Documento;

			_logger.LogInformation($"Creando acceso para el docente {request.DocenteID}...");
			var passwordHashed = _servicioSeguridad.HashPassword(dni);
			var salt = passwordHashed.GetValueOrDefault("salt");
			var hash = passwordHashed.GetValueOrDefault("hash");

			_logger.LogInformation($"SALT: {salt}");
			_logger.LogInformation($"HASH: {hash}");

			var existeUsuario = _unitOfWork.Usuarios.ExisteUsuarioDelDocente(request.DocenteID);
			if (existeUsuario)
			{
				_logger.LogInformation($"Reestableciendo acceso para el docente {request.DocenteID}...");
				var usuario = (await _unitOfWork.Usuarios
					.BuscarAsync(x => x.DocenteID.Equals(request.DocenteID)))
					.FirstOrDefault();

				usuario.CambiarPassword(salt, hash);

				_unitOfWork.Usuarios.Modificar(usuario);
			}
			else
			{
				_logger.LogInformation($"Creando acceso para el docente {request.DocenteID}...");
				
				var usuario = new Usuario(docente.Id, docente.Email, salt, hash);
				
				await _unitOfWork.Usuarios.AgregarAsync(usuario);
			}

			await _unitOfWork.GuardarCambiosAsync();
			_logger.LogInformation("Acceso creado correctamente...");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public async Task RestablecerAccesoAsync(RestablecerAccesoRequest request)
	{
		if (request is null)
		{
			throw new NullReferenceException("Datos incompletos para restablecer la clave de identidad del usuario.");
		}

		try
		{
			_logger.LogInformation($"Recuperando acceso del usuario...");
			var usuario = _unitOfWork.Usuarios.BuscarPorEmail(request.Email);
			if (usuario is null)
			{
				throw new ArgumentException("Datos de acceso incorrectos.");
			}

			var docente = await _unitOfWork.Docentes.BuscarPorIDAsync(usuario.DocenteID);

			var legajo = docente.Legajo;
			var passwordHashed = _servicioSeguridad.HashPassword(legajo);

			var salt = passwordHashed.GetValueOrDefault("salt");
			var hash = passwordHashed.GetValueOrDefault("hash");

			usuario.CambiarPassword(salt, hash);

			_unitOfWork.Usuarios.Modificar(usuario);
			await _unitOfWork.GuardarCambiosAsync();

			_logger.LogInformation($"Acceso restablecido...");
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"\nExcepción generada: {ex.Message}\n");
			throw;
		}
	}

	public void ActualizarRol(ActualizarRolRequest request)
	{
		throw new NotImplementedException();
	}
}
