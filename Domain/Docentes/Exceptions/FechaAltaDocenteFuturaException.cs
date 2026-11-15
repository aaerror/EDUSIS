namespace Domain.Docentes.Exceptions;

public class FechaAltaDocenteFuturaException : Exception
{
	private const string ERROR = "La fecha de alta del docente no puede ser posterior al día de hoy.";


	public FechaAltaDocenteFuturaException() : base(ERROR) { }
	public FechaAltaDocenteFuturaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public FechaAltaDocenteFuturaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
