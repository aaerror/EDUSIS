namespace Domain.Divisiones.Exceptions;

public class DivisionNoEncontradaException : Exception
{
	private const string ERROR = "No se encontró la división.";


	public DivisionNoEncontradaException()
		: base(ERROR) {}
	public DivisionNoEncontradaException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public DivisionNoEncontradaException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
