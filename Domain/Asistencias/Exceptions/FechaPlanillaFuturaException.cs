namespace Domain.Asistencias.Exceptions;

internal class FechaPlanillaFuturaException : Exception
{
	private const string ERROR = "La fecha de la planilla no puede ser posterior a hoy.";


	public FechaPlanillaFuturaException()
		: base(ERROR) { }
	public FechaPlanillaFuturaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public FechaPlanillaFuturaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
