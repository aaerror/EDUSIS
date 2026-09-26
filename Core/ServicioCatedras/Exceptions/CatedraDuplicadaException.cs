namespace Core.ServicioCatedras.Exceptions;

public class CatedraDuplicadaException : Exception
{
	private const string ERROR = "Ya existe una cátedra para esa materia en esa división.";


	public CatedraDuplicadaException() : base(ERROR) { }
}
