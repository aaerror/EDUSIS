namespace Domain.Divisiones.Exceptions;

internal class DivisionNoEliminableException : Exception
{
	private const string ERROR = "La división no se puede eliminar.";


	public DivisionNoEliminableException()
		: base(ERROR) { }
	public DivisionNoEliminableException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DivisionNoEliminableException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
