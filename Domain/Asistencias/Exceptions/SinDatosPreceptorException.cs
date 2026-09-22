namespace Domain.Asistencias.Exceptions;

internal class SinDatosPreceptorException : Exception
{
	private const string ERROR = "Preceptor sin especificar.";


	public SinDatosPreceptorException()
		: base(ERROR) { }
	public SinDatosPreceptorException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public SinDatosPreceptorException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
