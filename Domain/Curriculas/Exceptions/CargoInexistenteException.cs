namespace Domain.Curriculas.Exceptions;

internal class CargoInexistenteException : Exception
{
	private const string ERROR = "Cargo docente es inexistente.";


	public CargoInexistenteException()
		: base(ERROR) {}
	public CargoInexistenteException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public CargoInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) { }
}