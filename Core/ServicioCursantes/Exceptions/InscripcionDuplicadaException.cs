namespace Core.ServicioCursantes.Exceptions;

public class InscripcionDuplicadaException : Exception
{
	private const string ERROR = "El alumno ya se encuentra inscripto en el ciclo lectivo indicado.";


	public InscripcionDuplicadaException() : base(ERROR) { }
}
