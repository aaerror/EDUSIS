namespace Domain.Licencias.Exceptions;

internal class LicenciaIndefinidaException : Exception
{
	private const string ERROR = "La licencia no tiene una fecha de finalización prevista.";


	public LicenciaIndefinidaException()
		: base() { }

	public LicenciaIndefinidaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }

	public LicenciaIndefinidaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}