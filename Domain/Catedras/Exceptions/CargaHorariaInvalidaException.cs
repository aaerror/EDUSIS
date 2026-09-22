namespace Domain.Catedras.Exceptions;

internal class CargaHorariaInvalidaException : Exception
{
	private const string ERROR = "La carga horaria de la cátedra debe ser mayor a cero.";


	public CargaHorariaInvalidaException()
		: base(ERROR) { }
	public CargaHorariaInvalidaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CargaHorariaInvalidaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
