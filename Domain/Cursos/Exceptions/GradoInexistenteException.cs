namespace Domain.Cursos.Exceptions;

public class GradoInexistenteException : Exception
{
	private const string ERROR = "El grado indicado no existe.";


	public GradoInexistenteException() : base(ERROR) { }
	public GradoInexistenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public GradoInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
