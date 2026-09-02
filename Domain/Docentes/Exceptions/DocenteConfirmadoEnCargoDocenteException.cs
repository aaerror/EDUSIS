namespace Domain.Docentes.Exceptions;

internal class DocenteConfirmadoEnCargoDocenteException : Exception
{
	private const string ERROR = "El docente ya se encuentra confirmado en el cargo docente.";


	public DocenteConfirmadoEnCargoDocenteException() : base() { }
	public DocenteConfirmadoEnCargoDocenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocenteConfirmadoEnCargoDocenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}