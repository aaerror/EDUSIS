namespace Domain.Catedras.Horarios.Exceptions;

public class TurnoInexistenteException : Exception
{
	private const string ERROR = "El turno indicado no existe.";


	public TurnoInexistenteException() : base(ERROR) { }
	public TurnoInexistenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public TurnoInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
