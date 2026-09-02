namespace Domain.Docentes.Exceptions;

internal class DocenteMenorDeEdadException : Exception
{
	private const string ERROR = "El docente no cumplió la mayoría de edad.";


	public DocenteMenorDeEdadException() : base() { }
	public DocenteMenorDeEdadException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocenteMenorDeEdadException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}