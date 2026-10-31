namespace Domain.Divisiones.Exceptions;

public class SinLetrasDisponiblesException : Exception
{
	private const string ERROR = "El curso agotó las letras disponibles para sus divisiones.";


	public SinLetrasDisponiblesException()
		: base(ERROR) { }
	public SinLetrasDisponiblesException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public SinLetrasDisponiblesException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
