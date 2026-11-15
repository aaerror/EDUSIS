namespace Core.ServicioDocentes.Exceptions;

public class CuilDocenteDuplicadoException : Exception
{
	private const string ERROR = "El CUIL del docente ya se encuentra registrado.";


	public CuilDocenteDuplicadoException() : base(ERROR) { }
}
