namespace Domain.Cursantes.Exceptions;

public class PeriodoInexistenteException : Exception
{
	private const string ERROR = "El período de calificación indicado no existe.";


	public PeriodoInexistenteException() : base(ERROR) { }
	public PeriodoInexistenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PeriodoInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
