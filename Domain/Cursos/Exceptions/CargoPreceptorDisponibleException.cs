namespace Domain.Cursos.Exceptions;

internal class CargoPreceptorDisponibleException : Exception
{
	private const string ERROR = "El cargo de preceptor se encuentra disponible.";


	public CargoPreceptorDisponibleException()
		: base(ERROR) { }
	public CargoPreceptorDisponibleException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CargoPreceptorDisponibleException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}