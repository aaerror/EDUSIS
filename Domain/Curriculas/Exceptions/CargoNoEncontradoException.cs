namespace Domain.Curriculas.Exceptions;

internal class CargoNoEncontradoException : Exception
{
	private const string ERROR = "Cargo docente no encontrado.";


	public CargoNoEncontradoException()
		: base(ERROR) { }
	public CargoNoEncontradoException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: {parametro}")) { }
	public CargoNoEncontradoException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) { }
}