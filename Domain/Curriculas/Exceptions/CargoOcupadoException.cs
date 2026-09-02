namespace Domain.Curriculas.Exceptions;

internal class CargoOcupadoException : Exception
{
	private const string ERROR = "Ya se encuentra un docente con este cargo docente en la materia.";


	public CargoOcupadoException()
		: base(ERROR) { }
	public CargoOcupadoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CargoOcupadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}