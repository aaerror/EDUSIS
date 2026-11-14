namespace Core.ServicioAutenticaciones.Exceptions;

/// <summary>
/// El ingreso al sistema falló. <strong>Un solo tipo para las dos causas</strong> —el usuario no existe
/// y la contraseña no valida— y con un solo mensaje, que es el mismo que tenían los dos
/// <c>ArgumentException</c> que reemplaza.
/// </summary>
/// <remarks>
/// <para>
/// <strong>No partir esto en dos excepciones.</strong> Que las dos causas sean indistinguibles para el
/// llamador es una propiedad de seguridad deliberada, no una imprecisión: dos tipos distintos —o dos
/// mensajes distintos— le permitirían a quien pruebe credenciales averiguar si un nombre de usuario
/// existe, que es el primer paso de un ataque por enumeración. El sistema responde lo mismo para
/// "ese usuario no existe" y para "esa contraseña está mal".
/// </para>
/// <para>
/// Por el mismo motivo el mensaje no nombra cuál de los dos datos falló, y <c>ServicioUsuario</c> la
/// reutiliza en el restablecimiento de acceso en lugar de declarar una propia.
/// </para>
/// </remarks>
public class CredencialesInvalidasException : Exception
{
	private const string ERROR = "Datos de acceso incorrectos.";


	public CredencialesInvalidasException() : base(ERROR) { }
}
