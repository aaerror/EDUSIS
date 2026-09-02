namespace Domain.Docentes.Exceptions;

internal class DocenteInactivoException : Exception
{
	private const string ERROR = "El docente se encuentra inactivo.";


	public DocenteInactivoException() : base() { }
	public DocenteInactivoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocenteInactivoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}