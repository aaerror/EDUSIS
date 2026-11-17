namespace Domain.Usuarios.Exceptions;

public class NombreUsuarioRequeridoException : Exception
{
	private const string ERROR = "Se debe asignar un nombre de usuario.";


	public NombreUsuarioRequeridoException()
		: base(ERROR) { }

	public NombreUsuarioRequeridoException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public NombreUsuarioRequeridoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
