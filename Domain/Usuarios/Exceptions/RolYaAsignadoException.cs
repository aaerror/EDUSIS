namespace Domain.Usuarios.Exceptions;

public class RolYaAsignadoException : Exception
{
	private const string ERROR = "El rol ya se encuentra asignado al usuario.";


	public RolYaAsignadoException()
		: base(ERROR) { }

	public RolYaAsignadoException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public RolYaAsignadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
