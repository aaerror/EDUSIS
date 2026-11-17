namespace Domain.Usuarios.Exceptions;

public class UsuarioSinDocenteException : Exception
{
	private const string ERROR = "Se debe asignar un docente al usuario.";


	public UsuarioSinDocenteException()
		: base(ERROR) { }

	public UsuarioSinDocenteException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public UsuarioSinDocenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
