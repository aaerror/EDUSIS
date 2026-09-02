namespace Domain.Curriculas.Exceptions;

internal class CargoNoIniciadoException : Exception
{
	private const string ERROR = "El cargo docente no ha iniciado.";

	public CargoNoIniciadoException()
		: base(ERROR) { }
	public CargoNoIniciadoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CargoNoIniciadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}