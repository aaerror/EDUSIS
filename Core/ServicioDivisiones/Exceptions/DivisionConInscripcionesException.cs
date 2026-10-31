namespace Core.ServicioDivisiones.Exceptions;

public class DivisionConInscripcionesException : Exception
{
	private const string ERROR = "La división tiene cursantes inscriptos: dar de baja o trasladar a los cursantes antes de eliminar la división.";


	public DivisionConInscripcionesException() : base(ERROR) { }
}
