namespace Domain.Docentes.Exceptions;

public class DocenteNoEncontradoException : Exception
{
	private const string ERROR = "No se encontró el docente.";


	public DocenteNoEncontradoException()
		: base(ERROR) {}
	public DocenteNoEncontradoException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public DocenteNoEncontradoException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
