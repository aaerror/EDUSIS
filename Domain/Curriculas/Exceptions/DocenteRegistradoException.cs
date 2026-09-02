namespace Domain.Curriculas.Exceptions;

internal class DocenteRegistradoException : Exception
{
	private const string ERROR = "El docente ya se encuentra con un cargo docente en la asignatura el cuál todavía no ha finalizado.";


	public DocenteRegistradoException()
		: base(ERROR) { }
	public DocenteRegistradoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocenteRegistradoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}