namespace Domain.Catedras.Exceptions;

public class CatedraNoEncontradaException : Exception
{
	private const string ERROR = "No se encontró la cátedra.";


	public CatedraNoEncontradaException()
		: base(ERROR) {}
	public CatedraNoEncontradaException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public CatedraNoEncontradaException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
