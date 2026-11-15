namespace Domain.Catedras.Horarios.Exceptions;

public class DiaInexistenteException : Exception
{
	private const string ERROR = "El día de la semana indicado no existe.";


	public DiaInexistenteException() : base(ERROR) { }
	public DiaInexistenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DiaInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
