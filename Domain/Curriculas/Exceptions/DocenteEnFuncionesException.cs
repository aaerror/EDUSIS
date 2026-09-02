namespace Domain.Curriculas.Exceptions;

internal class DocenteEnFuncionesException : Exception
{
	private const string ERROR = "El docente ya se encuentra como docente de aula.";


	public DocenteEnFuncionesException()
		: base(ERROR) { }
	public DocenteEnFuncionesException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocenteEnFuncionesException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}