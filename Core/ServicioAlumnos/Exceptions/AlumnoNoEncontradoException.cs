namespace Core.ServicioAlumnos.Exceptions;

public class AlumnoNoEncontradoException : Exception
{
	private const string ERROR = "No se encontró el alumno.";


	public AlumnoNoEncontradoException()
		: base(ERROR) {}
	public AlumnoNoEncontradoException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public AlumnoNoEncontradoException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
