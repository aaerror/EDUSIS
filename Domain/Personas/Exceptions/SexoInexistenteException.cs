namespace Domain.Personas.Exceptions;

public class SexoInexistenteException : Exception
{
	private const string ERROR = "El sexo indicado no existe.";


	public SexoInexistenteException() : base(ERROR) { }
	public SexoInexistenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public SexoInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
