namespace Domain.Docentes.Exceptions;

public class LegajoDocenteInvalidoException : Exception
{
	private const string ERROR = "El legajo del docente debe tener 6 dígitos.";


	public LegajoDocenteInvalidoException() : base(ERROR) { }
	public LegajoDocenteInvalidoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public LegajoDocenteInvalidoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
