namespace Domain.Personas.Exceptions;

public class ApellidoRequeridoException : Exception
{
	private const string ERROR = "Se debe ingresar el apellido.";


	public ApellidoRequeridoException() : base(ERROR) { }
	public ApellidoRequeridoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public ApellidoRequeridoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
