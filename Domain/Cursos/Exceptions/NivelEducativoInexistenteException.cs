namespace Domain.Cursos.Exceptions;

public class NivelEducativoInexistenteException : Exception
{
	private const string ERROR = "El nivel educativo indicado no existe.";


	public NivelEducativoInexistenteException() : base(ERROR) { }
	public NivelEducativoInexistenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public NivelEducativoInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
