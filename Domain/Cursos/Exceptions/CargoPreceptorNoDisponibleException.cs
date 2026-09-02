namespace Domain.Cursos.Exceptions;

internal class CargoPreceptorNoDisponibleException : Exception
{
	private const string ERROR = "El cargo de preceptor no se encuentra disponible.";


	public CargoPreceptorNoDisponibleException()
		: base(ERROR) { }
	public CargoPreceptorNoDisponibleException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CargoPreceptorNoDisponibleException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}