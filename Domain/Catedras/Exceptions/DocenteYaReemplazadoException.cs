namespace Domain.Catedras.Exceptions;

internal class DocenteYaReemplazadoException : Exception
{
	private const string ERROR = "El docente ya tiene una suplencia vigente.";


	public DocenteYaReemplazadoException()
		: base(ERROR) {}
	public DocenteYaReemplazadoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) {}
	public DocenteYaReemplazadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) {}
}
