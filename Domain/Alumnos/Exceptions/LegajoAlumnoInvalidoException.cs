namespace Domain.Alumnos.Exceptions;

public class LegajoAlumnoInvalidoException : Exception
{
	private const string ERROR = "El legajo del alumno debe tener dos letras mayúsculas seguidas de 4 dígitos (por ejemplo, AL0001).";


	public LegajoAlumnoInvalidoException() : base(ERROR) { }
	public LegajoAlumnoInvalidoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public LegajoAlumnoInvalidoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
