namespace Domain.Licencias.Exceptions;

public class ArticuloInexistenteException : Exception
{
	private const string ERROR = "El artículo de licencia indicado no existe.";


	public ArticuloInexistenteException() : base(ERROR) { }
	public ArticuloInexistenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public ArticuloInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
