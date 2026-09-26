namespace Core.ServicioMaterias.Exceptions;

public class LimiteHorasSemanalesException : Exception
{
	private const string ERROR = "Se alcanzó el límite de horas cátedra semanales de la currícula.";


	public LimiteHorasSemanalesException() : base(ERROR) { }
}
