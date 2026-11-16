namespace Domain.Personas.Exceptions;

public class CalleRequeridaException : Exception
{
	private const string ERROR = "Se debe ingresar la calle del domicilio.";


	public CalleRequeridaException() : base(ERROR) { }
	public CalleRequeridaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CalleRequeridaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
