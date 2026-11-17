namespace Domain.Personas.Exceptions;

public class TelefonoRequeridoException : Exception
{
	private const string ERROR = "Se debe ingresar el teléfono.";


	public TelefonoRequeridoException()
		: base(ERROR) { }

	public TelefonoRequeridoException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public TelefonoRequeridoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
