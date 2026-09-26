namespace Core.ServicioDivisiones.Exceptions;

public class PreceptorAsignadoException : Exception
{
	private const string ERROR = "El docente ya se encuentra asignado como preceptor de otra división.";


	public PreceptorAsignadoException() : base(ERROR) { }
}
