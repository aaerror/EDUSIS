namespace Domain.Asistencias.Exceptions;

internal class PlanillaSinCursantesException : Exception
{
	private const string ERROR = "La planilla debe contener al menos un cursante.";


	public PlanillaSinCursantesException()
		: base(ERROR) { }
	public PlanillaSinCursantesException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PlanillaSinCursantesException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
