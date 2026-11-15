namespace Domain.Licencias.Exceptions;

public class LicenciaConFechaDeFinalizacionException : Exception
{
	private const string ERROR = "La licencia ya posee una fecha de finalización.";


	public LicenciaConFechaDeFinalizacionException()
		: base(ERROR) { }

	public LicenciaConFechaDeFinalizacionException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public LicenciaConFechaDeFinalizacionException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
