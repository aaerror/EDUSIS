namespace Domain.Docentes.Exceptions;

public class AltaActivaConInicioFuturoException : Exception
{
	private const string ERROR = "Un puesto que inicia después de hoy se da de alta como pendiente y se activa al tomar posesión.";


	public AltaActivaConInicioFuturoException() : base(ERROR) { }
	public AltaActivaConInicioFuturoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public AltaActivaConInicioFuturoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
