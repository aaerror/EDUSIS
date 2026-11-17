namespace Core.ServicioUsuarios.DTOs.Requests;

/// <summary>
/// Cuenta a la que un administrador le restablece la clave inicial (el documento del docente).
/// Va por <c>UsuarioID</c>, que es lo que tiene elegido la pantalla de Usuarios.
/// </summary>
public record RestablecerAccesoRequest(Guid UsuarioID);