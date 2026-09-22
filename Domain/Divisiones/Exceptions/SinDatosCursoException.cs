namespace Domain.Divisiones.Exceptions;

internal class SinDatosCursoException : Exception
{
	private const string ERROR = "Curso sin especificar.";


	public SinDatosCursoException()
		: base(ERROR) { }
	public SinDatosCursoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public SinDatosCursoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
