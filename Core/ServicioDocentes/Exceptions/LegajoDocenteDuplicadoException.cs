namespace Core.ServicioDocentes.Exceptions;

public class LegajoDocenteDuplicadoException : Exception
{
	private const string ERROR = "El legajo docente ya se encuentra registrado.";


	public LegajoDocenteDuplicadoException() : base(ERROR) { }
}
