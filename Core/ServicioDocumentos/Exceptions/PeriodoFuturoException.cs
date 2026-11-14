namespace Core.ServicioDocumentos.Exceptions;

/// <summary>
/// El período pedido para el certificado empieza en el futuro. Un certificado acredita algo ya
/// ocurrido, así que no se emite sobre un período que todavía no empezó.
/// </summary>
public class PeriodoFuturoException : Exception
{
	private const string ERROR = "El período del certificado no puede comenzar en el futuro.";


	public PeriodoFuturoException() : base(ERROR) { }
}
