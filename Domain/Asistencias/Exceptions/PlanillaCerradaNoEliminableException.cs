namespace Domain.Asistencias.Exceptions;

public class PlanillaCerradaNoEliminableException : Exception
{
	private const string ERROR = "Una planilla cerrada es registro histórico y no se puede eliminar.";


	public PlanillaCerradaNoEliminableException()
		: base(ERROR) { }
	public PlanillaCerradaNoEliminableException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PlanillaCerradaNoEliminableException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
