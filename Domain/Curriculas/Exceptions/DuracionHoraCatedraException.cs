namespace Domain.Curriculas.Exceptions;

internal class DuracionHoraCatedraException : Exception
{
	private const string ERROR = "Duración de hora cátedra inválida.";


	public DuracionHoraCatedraException()
		: base(ERROR) { }
	public DuracionHoraCatedraException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DuracionHoraCatedraException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}