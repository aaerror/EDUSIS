namespace Domain.Catedras.Exceptions;

internal class SuplenciaSinReemplazoException : Exception
{
	private const string ERROR = "Una suplencia debe indicar a quién reemplaza.";


	public SuplenciaSinReemplazoException()
		: base(ERROR) {}
	public SuplenciaSinReemplazoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) {}
	public SuplenciaSinReemplazoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) {}
}
