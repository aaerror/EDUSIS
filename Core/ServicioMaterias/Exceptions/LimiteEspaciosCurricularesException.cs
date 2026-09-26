namespace Core.ServicioMaterias.Exceptions;

public class LimiteEspaciosCurricularesException : Exception
{
	private const string ERROR = "Se alcanzó el límite de espacios curriculares de la currícula.";


	public LimiteEspaciosCurricularesException() : base(ERROR) { }
}
