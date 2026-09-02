namespace Domain.Curriculas.Exceptions;

internal class NotaIncorrectaException : Exception
{
	private const string ERROR = "Nota inválida. La nota mínima es un uno (1) y la nota máxima es un diez (10).";


	public NotaIncorrectaException()
		: base(ERROR) { }
	public NotaIncorrectaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public NotaIncorrectaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}