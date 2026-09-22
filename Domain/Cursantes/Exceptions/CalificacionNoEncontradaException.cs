namespace Domain.Cursantes.Exceptions;

internal class CalificacionNoEncontradaException : Exception
{
	private const string ERROR = "Calificación no encontrada.";


	public CalificacionNoEncontradaException()
		: base(ERROR) { }
	public CalificacionNoEncontradaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CalificacionNoEncontradaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
