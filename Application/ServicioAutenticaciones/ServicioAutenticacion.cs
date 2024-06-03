using Core.ServicioAutenticaciones.DTOs.Request;
using Core.ServicioAutenticaciones.DTOs.Response;
using Core.ServicioSeguridad;
using Domain.Usuarios;
using Infrastructure.Shared;

namespace Core.ServicioAutenticaciones;

public class ServicioAutenticacion : IServicioAutenticacion
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServicioSeguridad _servicioSeguridad;

    public ServicioAutenticacion(IUnitOfWork unitOfWork, IServicioSeguridad servicioSeguridad)
    {
        _unitOfWork = unitOfWork;
        _servicioSeguridad = servicioSeguridad;
    }

    public void RegistrarUsuario(RegistrarUsuarioRequest request)
    {
        if (request is null)
        {
            throw new NullReferenceException("Datos incompletos para registrar el usuario.");
        }

        try
        {
            if (!_unitOfWork.Docentes.ExisteID(request.DocenteID))
            {
                throw new ArgumentException("No se encontró el docente.", nameof(request.DocenteID));
            }

            if (_unitOfWork.Usuarios.EsUsuarioInvalido(request.Usuario))
            {
                throw new ArgumentException("El nombre de usuario ya se encuentra en uso.", nameof(request.Usuario));
            }

            if (_unitOfWork.Usuarios.ExisteUsuarioDelDocente(request.DocenteID))
            {
                throw new ArgumentException("El docente ya cuenta con un usuario en el sistema.", nameof(request.DocenteID));
            }

            string saltAndHash = _servicioSeguridad.HashPassword(request.Clave);
            char[] delimiter = { ':' };
            string[] split = saltAndHash.Split(delimiter);

            var usuario = new Usuario(request.DocenteID, request.Usuario, split[0], split[1], request.Rol);

            _unitOfWork.Usuarios.AgregarAsync(usuario);
            _unitOfWork.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public UsuarioResponse Login(LoginRequest request)
    {
        string salt = string.Empty;
        string hash = string.Empty;

        if (request is null)
        {
            throw new NullReferenceException("Datos incompletos para acceder al sistema.");
        }

        try
        {
            _unitOfWork.Usuarios.RecuperarDatosAcceso(request.Usuario, out salt, out hash);
            
            var esAccesoValido = _servicioSeguridad.ValidatePassword(request.Clave, salt, hash);
            if (!esAccesoValido)
            {
                throw new ArgumentException("Datos de acceso incorrectos", nameof(request));
            }

            var usuario = _unitOfWork.Usuarios.BuscarPorUsuario(request.Usuario) ?? throw new NullReferenceException("Usuario no registrado en el sistema");

            return new UsuarioResponse(usuario.DocenteID, usuario.Rol, usuario.Accesos);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}