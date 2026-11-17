namespace Domain.Catedras.Exceptions;

public class TerminoDeCargoNoPosteriorAHoyException : Exception
{
	private const string ERROR = "La fecha de finalización debe ser posterior al día de hoy.";


	public TerminoDeCargoNoPosteriorAHoyException()
		: base(ERROR) { }

	public TerminoDeCargoNoPosteriorAHoyException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public TerminoDeCargoNoPosteriorAHoyException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
