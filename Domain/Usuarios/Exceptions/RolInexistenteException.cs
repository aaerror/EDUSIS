namespace Domain.Usuarios.Exceptions;

public class RolInexistenteException : Exception
{
	private const string ERROR = "El rol indicado no existe en el catálogo.";


	public RolInexistenteException()
		: base(ERROR) { }

	public RolInexistenteException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public RolInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
