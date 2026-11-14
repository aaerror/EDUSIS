namespace Core.ServicioCursantes.Exceptions;

public class CicloLectivoNoEncontradoException : Exception
{
	private const string ERROR = "No se encontró el ciclo lectivo.";


	public CicloLectivoNoEncontradoException()
		: base(ERROR) {}
	public CicloLectivoNoEncontradoException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public CicloLectivoNoEncontradoException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
