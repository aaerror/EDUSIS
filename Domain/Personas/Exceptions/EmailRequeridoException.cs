namespace Domain.Personas.Exceptions;

public class EmailRequeridoException : Exception
{
	private const string ERROR = "Se debe ingresar el email.";


	public EmailRequeridoException()
		: base(ERROR) { }

	public EmailRequeridoException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public EmailRequeridoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
