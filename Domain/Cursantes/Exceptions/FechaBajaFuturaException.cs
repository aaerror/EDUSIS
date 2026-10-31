namespace Domain.Cursantes.Exceptions;

public class FechaBajaFuturaException : Exception
{
	private const string ERROR = "La fecha de baja no puede ser posterior a hoy.";


	public FechaBajaFuturaException()
		: base(ERROR) { }
	public FechaBajaFuturaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public FechaBajaFuturaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
