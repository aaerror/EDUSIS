namespace Domain.Asistencias.Exceptions;

internal class ObservacionExtensaException : Exception
{
	private const string ERROR = "La observación no debe exceder 140 caracteres.";


	public ObservacionExtensaException()
		: base(ERROR) { }
	public ObservacionExtensaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public ObservacionExtensaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
