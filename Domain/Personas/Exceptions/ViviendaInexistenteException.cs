namespace Domain.Personas.Exceptions;

public class ViviendaInexistenteException : Exception
{
	private const string ERROR = "El tipo de vivienda indicado no existe.";


	public ViviendaInexistenteException() : base(ERROR) { }
	public ViviendaInexistenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public ViviendaInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
