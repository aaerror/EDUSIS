namespace Domain.Cursantes.Calificaciones.Exceptions;

public class NotaRequeridaException : Exception
{
	private const string ERROR = "Debe especificar la nota del alumno para registrar una calificación.";


	public NotaRequeridaException()
		: base(ERROR) { }
	public NotaRequeridaException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }
	public NotaRequeridaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
