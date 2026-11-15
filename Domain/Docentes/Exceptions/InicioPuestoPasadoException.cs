namespace Domain.Docentes.Exceptions;

public class InicioPuestoPasadoException : Exception
{
	private const string ERROR = "La fecha de alta del cargo docente no puede ser anterior al día de la fecha.";


	public InicioPuestoPasadoException() : base(ERROR) { }
	public InicioPuestoPasadoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public InicioPuestoPasadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
