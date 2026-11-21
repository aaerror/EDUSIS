namespace Domain.Licencias.Exceptions;

public class LicenciaIndefinidaNoAdmitidaException : Exception
{
	private const string ERROR = "Este tipo de licencia tiene un plazo legal: debe indicarse la cantidad de días.";


	public LicenciaIndefinidaNoAdmitidaException() : base(ERROR) { }
	public LicenciaIndefinidaNoAdmitidaException(PlazoLegal plazo)
		: base($"{ERROR} {plazo.Denominacion} ({plazo.Norma}) no puede quedar sin fecha de fin.") { }
	public LicenciaIndefinidaNoAdmitidaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public LicenciaIndefinidaNoAdmitidaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
