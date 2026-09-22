namespace Domain.Divisiones.Exceptions;

internal class DivisionConMatriculaSuficienteException : Exception
{
	private const string ERROR = "La división cuenta con matrícula suficiente y no se puede eliminar.";


	public DivisionConMatriculaSuficienteException()
		: base(ERROR) { }
	public DivisionConMatriculaSuficienteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DivisionConMatriculaSuficienteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
