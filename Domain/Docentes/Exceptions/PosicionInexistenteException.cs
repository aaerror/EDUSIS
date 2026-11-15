namespace Domain.Docentes.Exceptions;

public class PosicionInexistenteException : Exception
{
	private const string ERROR = "La posición de puesto docente indicada no existe.";


	public PosicionInexistenteException() : base(ERROR) { }
	public PosicionInexistenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PosicionInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
