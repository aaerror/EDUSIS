namespace Domain.Asistencias.Exceptions;

public class SinDatosCursanteException : Exception
{
	private const string ERROR = "Cursante sin especificar.";


	public SinDatosCursanteException()
		: base(ERROR) { }
	public SinDatosCursanteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public SinDatosCursanteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
