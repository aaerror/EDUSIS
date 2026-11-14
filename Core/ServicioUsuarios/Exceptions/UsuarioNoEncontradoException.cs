namespace Core.ServicioUsuarios.Exceptions;

public class UsuarioNoEncontradoException : Exception
{
	private const string ERROR = "No se encontró el usuario.";


	public UsuarioNoEncontradoException()
		: base(ERROR) {}
	public UsuarioNoEncontradoException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public UsuarioNoEncontradoException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
