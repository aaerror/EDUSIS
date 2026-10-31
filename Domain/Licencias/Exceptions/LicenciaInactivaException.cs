namespace Domain.Licencias.Exceptions;

public class LicenciaInactivaException : Exception
{
	private const string ERROR = "La licencia se encuentra inactiva.";


	public LicenciaInactivaException() : base() { }

	public LicenciaInactivaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }

	public LicenciaInactivaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}