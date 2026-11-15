namespace Domain.Personas.Exceptions;

public class DocumentoRequeridoException : Exception
{
	private const string ERROR = "Se debe ingresar el DNI.";


	public DocumentoRequeridoException() : base(ERROR) { }
	public DocumentoRequeridoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public DocumentoRequeridoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
