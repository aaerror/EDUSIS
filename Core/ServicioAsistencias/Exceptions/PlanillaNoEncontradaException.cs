namespace Core.ServicioAsistencias.Exceptions;

public class PlanillaNoEncontradaException : Exception
{
	private const string ERROR = "No se encontró la planilla de asistencia.";


	public PlanillaNoEncontradaException()
		: base(ERROR) {}
	public PlanillaNoEncontradaException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public PlanillaNoEncontradaException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
