namespace Core.ServicioCursantes.Exceptions;

public class DivisionDestinoInvalidaException : Exception
{
	private const string ERROR = "El cursante ya está inscripto en la división indicada.";


	public DivisionDestinoInvalidaException() : base(ERROR) { }
}
