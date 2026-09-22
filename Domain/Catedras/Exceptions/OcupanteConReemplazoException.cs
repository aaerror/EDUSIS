namespace Domain.Catedras.Exceptions;

internal class OcupanteConReemplazoException : Exception
{
	private const string ERROR = "Un cargo titular o interino no reemplaza a otra designación.";


	public OcupanteConReemplazoException()
		: base(ERROR) {}
	public OcupanteConReemplazoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) {}
	public OcupanteConReemplazoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) {}
}
