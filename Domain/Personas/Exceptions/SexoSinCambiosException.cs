namespace Domain.Personas.Exceptions;

public class SexoSinCambiosException : Exception
{
	private const string ERROR = "El sexo ingresado es igual al registrado.";


	public SexoSinCambiosException() : base(ERROR) { }
	public SexoSinCambiosException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public SexoSinCambiosException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
