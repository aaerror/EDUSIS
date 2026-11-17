namespace Domain.Usuarios.Exceptions;

public class RolNoAsignadoException : Exception
{
	private const string ERROR = "El rol no se encuentra asignado al usuario.";


	public RolNoAsignadoException()
		: base(ERROR) { }

	public RolNoAsignadoException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public RolNoAsignadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
