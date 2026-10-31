namespace Domain.Catedras.Exceptions;

public class DocenteRegistradoException : Exception
{
	private const string ERROR = "El docente ya tiene un cargo vigente en la cátedra.";


	public DocenteRegistradoException()
		: base(ERROR) { }
	public DocenteRegistradoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocenteRegistradoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
