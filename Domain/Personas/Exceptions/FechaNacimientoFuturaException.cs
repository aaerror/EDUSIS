namespace Domain.Personas.Exceptions;

public class FechaNacimientoFuturaException : Exception
{
	private const string ERROR = "La fecha de nacimiento no puede ser posterior a la fecha actual.";


	public FechaNacimientoFuturaException() : base(ERROR) { }
	public FechaNacimientoFuturaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public FechaNacimientoFuturaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
