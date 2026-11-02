namespace Domain.Catedras.Exceptions;

/// <summary>
/// La otra mitad de <see cref="CargoTemporalSinFechaFinalizacionException"/>: esa prohíbe el cargo
/// a término sin término, y esta el término sobre el cargo que no lo lleva.
/// </summary>
/// <remarks>
/// No reusa <see cref="CargoConFechaFinalizacionException"/>, que dice "el cargo YA tiene fecha de
/// finalización" y es lo que contesta el pase a interino sobre una designación que ya es temporal.
/// Acá el problema es el contrario —se está PIDIENDO una fecha de fin sobre un cargo que no la
/// admite— y el mensaje viaja tal cual a la pantalla.
/// </remarks>
public class TitularConFechaFinalizacionException : Exception
{
	private const string ERROR = "El titular se designa por tiempo indeterminado: no lleva fecha de fin.";


	public TitularConFechaFinalizacionException()
		: base(ERROR) {}
	public TitularConFechaFinalizacionException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) {}
	public TitularConFechaFinalizacionException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) {}
}
