namespace Core.ServicioLicencias.Exceptions;

public class LicenciaNoEncontradaException : Exception
{
	private const string ERROR = "No se encontró la licencia.";


	public LicenciaNoEncontradaException()
		: base(ERROR) {}
	public LicenciaNoEncontradaException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public LicenciaNoEncontradaException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
