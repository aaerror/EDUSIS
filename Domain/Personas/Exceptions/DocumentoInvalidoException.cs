namespace Domain.Personas.Exceptions;

public class DocumentoInvalidoException : Exception
{
	private const string ERROR = "El DNI debe tener 7 u 8 dígitos, sin puntos ni espacios.";


	public DocumentoInvalidoException() : base(ERROR) { }
	public DocumentoInvalidoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocumentoInvalidoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
