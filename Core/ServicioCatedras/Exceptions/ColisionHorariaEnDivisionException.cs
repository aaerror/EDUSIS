namespace Core.ServicioCatedras.Exceptions;

public class ColisionHorariaEnDivisionException : Exception
{
	private const string ERROR = "El horario se superpone con otra cátedra de la misma división.";


	public ColisionHorariaEnDivisionException() : base(ERROR) { }
}
