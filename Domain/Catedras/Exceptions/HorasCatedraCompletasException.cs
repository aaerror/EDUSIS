namespace Domain.Catedras.Exceptions;

internal class HorasCatedraCompletasException : Exception
{
	private const string ERROR = "La cátedra ya tiene sus horas cátedra completas.";


	public HorasCatedraCompletasException()
		: base(ERROR) { }
	public HorasCatedraCompletasException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public HorasCatedraCompletasException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
