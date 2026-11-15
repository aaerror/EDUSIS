namespace Domain.Licencias.Exceptions;

public class ExtensionSinDiasException : Exception
{
	private const string ERROR = "Se debe especificar la cantidad de días que se quiere extender la licencia.";


	public ExtensionSinDiasException()
		: base(ERROR) { }

	public ExtensionSinDiasException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public ExtensionSinDiasException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
