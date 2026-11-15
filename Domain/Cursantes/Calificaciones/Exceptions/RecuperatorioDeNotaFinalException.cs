namespace Domain.Cursantes.Calificaciones.Exceptions;

public class RecuperatorioDeNotaFinalException : Exception
{
	private const string ERROR = "La nota final no admite recuperatorio: lo que se recupera son los cuatrimestres y los períodos de recuperación.";


	public RecuperatorioDeNotaFinalException()
		: base(ERROR) { }
	public RecuperatorioDeNotaFinalException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public RecuperatorioDeNotaFinalException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
