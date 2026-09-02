namespace Domain.Docentes.Exceptions;

internal class DocenteSinCargoDocenteException : Exception
{
	private const string ERROR = "El docente no posee un cargo docente en la institución.";


	public DocenteSinCargoDocenteException() : base() { }
	public DocenteSinCargoDocenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocenteSinCargoDocenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}