namespace Domain.Catedras.Exceptions;

internal class CargoOcupadoException : Exception
{
	private const string ERROR = "El cargo ya se encuentra ocupado.";


	public CargoOcupadoException()
		: base(ERROR) { }
	public CargoOcupadoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CargoOcupadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
