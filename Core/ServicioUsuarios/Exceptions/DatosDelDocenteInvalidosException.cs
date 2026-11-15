namespace Core.ServicioUsuarios.Exceptions;

/// <summary>
/// El legajo informado no corresponde al docente indicado. Es la verificación de identidad previa al
/// restablecimiento de acceso, y por eso su mensaje no dice cuál de los dos datos no coincide.
/// </summary>
public class DatosDelDocenteInvalidosException : Exception
{
	private const string ERROR = "Datos del docente incorrectos.";


	public DatosDelDocenteInvalidosException() : base(ERROR) { }
}
