namespace Domain.Cursantes.Calificaciones.Exceptions;

public class FechaDeExamenPosteriorAHoyException : Exception
{
	private const string ERROR = "La fecha del examen no puede ser posterior a la fecha de registro en sistema.";


	public FechaDeExamenPosteriorAHoyException()
		: base(ERROR) { }
	public FechaDeExamenPosteriorAHoyException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }
	public FechaDeExamenPosteriorAHoyException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
