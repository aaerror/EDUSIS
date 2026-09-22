namespace Domain.Asistencias.Exceptions;

internal class PlanillaAbiertaException : Exception
{
	private const string ERROR = "La planilla está abierta.";


	public PlanillaAbiertaException()
		: base(ERROR) { }
	public PlanillaAbiertaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PlanillaAbiertaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
