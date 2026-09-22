namespace Domain.Catedras.Exceptions;

internal class SituacionRevistaNoEncontradaException : Exception
{
	private const string ERROR = "La situación de revista no pertenece a la cátedra.";


	public SituacionRevistaNoEncontradaException()
		: base(ERROR) { }
	public SituacionRevistaNoEncontradaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public SituacionRevistaNoEncontradaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
