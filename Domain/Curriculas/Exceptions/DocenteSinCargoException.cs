namespace Domain.Curriculas.Exceptions;

internal class DocenteSinCargoException : Exception
{
	private const string ERROR = "El docente no tiene un cargo en la materia.";


	public DocenteSinCargoException()
		: base(ERROR) { }
	public DocenteSinCargoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocenteSinCargoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}