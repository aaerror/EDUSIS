namespace Domain.Cursantes.Exceptions;

public class SinDatosCicloLectivoException : Exception
{
	private const string ERROR = "Ciclo lectivo sin especificar.";


	public SinDatosCicloLectivoException()
		: base(ERROR) { }
	public SinDatosCicloLectivoException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }
	public SinDatosCicloLectivoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
