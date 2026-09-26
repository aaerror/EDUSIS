namespace Core.ServicioMaterias.Exceptions;

public class NombreMateriaDuplicadoException : Exception
{
	private const string ERROR = "Ya existe una materia con ese nombre en la currícula.";


	public NombreMateriaDuplicadoException() : base(ERROR) { }
}
