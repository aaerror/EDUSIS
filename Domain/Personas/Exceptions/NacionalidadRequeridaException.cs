namespace Domain.Personas.Exceptions;

public class NacionalidadRequeridaException : Exception
{
	private const string ERROR = "Se debe ingresar la nacionalidad.";


	public NacionalidadRequeridaException() : base(ERROR) { }
	public NacionalidadRequeridaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public NacionalidadRequeridaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
