namespace Domain.Personas.Exceptions;

public class NombreRequeridoException : Exception
{
	private const string ERROR = "Se debe ingresar el nombre.";


	public NombreRequeridoException() : base(ERROR) { }
	public NombreRequeridoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public NombreRequeridoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
