namespace Domain.Licencias.Exceptions;

public class FechaDeFinalizacionNoPosteriorAHoyException : Exception
{
	private const string ERROR = "La fecha de finalización debe ser posterior al día de hoy.";


	public FechaDeFinalizacionNoPosteriorAHoyException()
		: base(ERROR) { }

	public FechaDeFinalizacionNoPosteriorAHoyException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public FechaDeFinalizacionNoPosteriorAHoyException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
