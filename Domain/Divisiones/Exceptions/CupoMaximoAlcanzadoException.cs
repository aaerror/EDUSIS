namespace Domain.Divisiones.Exceptions;

internal class CupoMaximoAlcanzadoException : Exception
{
	private const string ERROR = "Se alcanzó el cupo máximo de alumnos de la división.";


	public CupoMaximoAlcanzadoException()
		: base(ERROR) { }
	public CupoMaximoAlcanzadoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CupoMaximoAlcanzadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
