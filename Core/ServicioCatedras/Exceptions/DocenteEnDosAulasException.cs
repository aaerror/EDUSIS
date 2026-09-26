namespace Core.ServicioCatedras.Exceptions;

public class DocenteEnDosAulasException : Exception
{
	private const string ERROR = "El docente en funciones ya tiene asignada otra cátedra en ese horario.";


	public DocenteEnDosAulasException() : base(ERROR) { }
}
