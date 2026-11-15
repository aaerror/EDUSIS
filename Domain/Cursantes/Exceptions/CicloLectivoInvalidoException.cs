namespace Domain.Cursantes.Exceptions;

public class CicloLectivoInvalidoException : Exception
{
	private const string ERROR = "El ciclo lectivo debe ser un año de 4 dígitos (por ejemplo, 2026).";


	public CicloLectivoInvalidoException() : base(ERROR) { }
	public CicloLectivoInvalidoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CicloLectivoInvalidoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
