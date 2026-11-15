namespace Core.ServicioUsuarios.Exceptions;

public class UsuarioDeDocenteDuplicadoException : Exception
{
	private const string ERROR = "El docente ya cuenta con un usuario en el sistema.";


	public UsuarioDeDocenteDuplicadoException() : base(ERROR) { }
}
