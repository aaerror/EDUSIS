namespace Domain.Licencias.Exceptions;

internal class LicenciaActivaException : Exception
{
	private const string ERROR = "La licencia se encuentra activa.";


	public LicenciaActivaException()
		: base() { }

	public LicenciaActivaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }

	public LicenciaActivaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}