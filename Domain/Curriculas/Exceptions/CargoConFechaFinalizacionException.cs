namespace Domain.Curriculas.Exceptions;

internal class CargoConFechaFinalizacionException : Exception
{
	private const string ERROR = "El cargo tiene fecha de finalización.";


	public CargoConFechaFinalizacionException()
		: base(ERROR) {}
	public CargoConFechaFinalizacionException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) {}
	public CargoConFechaFinalizacionException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) {}
}