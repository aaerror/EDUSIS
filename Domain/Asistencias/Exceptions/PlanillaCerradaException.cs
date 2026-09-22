namespace Domain.Asistencias.Exceptions;

internal class PlanillaCerradaException : Exception
{
	private const string ERROR = "La planilla está cerrada.";


	public PlanillaCerradaException()
		: base(ERROR) { }
	public PlanillaCerradaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PlanillaCerradaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
