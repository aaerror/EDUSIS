namespace Domain.Licencias.Exceptions;

public class PlazoMaximoDeLicenciaExcedidoException : Exception
{
	private const string ERROR = "La cantidad de días supera el máximo que fija la norma para este tipo de licencia.";


	public PlazoMaximoDeLicenciaExcedidoException() : base(ERROR) { }
	public PlazoMaximoDeLicenciaExcedidoException(PlazoLegal plazo)
		: base($"{ERROR} {plazo.Denominacion} ({plazo.Norma}) admite hasta {plazo.Dias} días.") { }
	public PlazoMaximoDeLicenciaExcedidoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PlazoMaximoDeLicenciaExcedidoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
