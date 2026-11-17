namespace Domain.Divisiones.Exceptions;

public class DescripcionDivisionInvalidaException : Exception
{
	private const string ERROR = "La división del curso debe ser una letra.";


	public DescripcionDivisionInvalidaException()
		: base(ERROR) { }

	public DescripcionDivisionInvalidaException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public DescripcionDivisionInvalidaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
