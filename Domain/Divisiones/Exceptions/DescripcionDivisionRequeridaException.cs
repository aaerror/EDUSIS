namespace Domain.Divisiones.Exceptions;

public class DescripcionDivisionRequeridaException : Exception
{
	private const string ERROR = "Se debe ingresar la letra de la división.";


	public DescripcionDivisionRequeridaException()
		: base(ERROR) { }

	public DescripcionDivisionRequeridaException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public DescripcionDivisionRequeridaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
