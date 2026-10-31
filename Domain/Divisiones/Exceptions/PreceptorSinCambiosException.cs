namespace Domain.Divisiones.Exceptions;

public class PreceptorSinCambiosException : Exception
{
	private const string ERROR = "El docente ya es el preceptor de la división.";


	public PreceptorSinCambiosException()
		: base(ERROR) { }
	public PreceptorSinCambiosException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PreceptorSinCambiosException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
