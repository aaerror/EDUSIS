namespace Domain.Asistencias.Exceptions;

internal class TardanzaSinMinutosException : Exception
{
	private const string ERROR = "La tardanza debe tener una duración mayor a cero.";


	public TardanzaSinMinutosException()
		: base(ERROR) { }
	public TardanzaSinMinutosException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public TardanzaSinMinutosException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
