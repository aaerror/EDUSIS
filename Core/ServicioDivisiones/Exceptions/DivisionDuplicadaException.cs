namespace Core.ServicioDivisiones.Exceptions;

public class DivisionDuplicadaException : Exception
{
	private const string ERROR = "La división ya se encuentra registrada en el curso.";


	public DivisionDuplicadaException() : base(ERROR) { }
}
