namespace Domain.Curriculas.Exceptions;

internal class CargoTemporalSinFechaFinalizacionException : Exception
{
	private const string ERROR = "El cargo temporal debe poseer una fecha de finalización.";


	public CargoTemporalSinFechaFinalizacionException()
		: base(ERROR) { }
	public CargoTemporalSinFechaFinalizacionException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CargoTemporalSinFechaFinalizacionException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}