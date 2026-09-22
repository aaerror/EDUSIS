namespace Domain.Catedras.Exceptions;

internal class SinDatosDivisionException : Exception
{
	private const string ERROR = "División sin especificar.";


	public SinDatosDivisionException()
		: base(ERROR) { }
	public SinDatosDivisionException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public SinDatosDivisionException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
