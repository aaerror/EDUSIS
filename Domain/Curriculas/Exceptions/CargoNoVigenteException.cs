namespace Domain.Curriculas.Exceptions;

internal class CargoNoVigenteException : Exception
{
	private const string ERROR = "El cargo no se encuentra vigente.";


	public CargoNoVigenteException()
		: base(ERROR) {}
	public CargoNoVigenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) {}
	public CargoNoVigenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) {}
}