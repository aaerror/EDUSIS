using System.Security;

namespace Core.ServicioAutenticaciones.DTOs.Requests;

/// <summary>
/// Credenciales del acceso. La clave viaja como <see cref="SecureString"/> porque es lo que
/// entrega el control de contraseña de la pantalla; antes el par iba envuelto en un
/// <c>System.Net.NetworkCredential</c>, que obligaba a todo consumidor de <c>Core</c> a construirlo.
/// </summary>
public record LoginRequest(string Usuario, SecureString Clave);
