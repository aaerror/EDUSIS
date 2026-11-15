namespace Domain.Licencias.Exceptions;

public class FechaDeInicioDeLicenciaFuturaException : Exception
{
	private const string ERROR = "La fecha de inicio de la licencia no puede ser posterior a la fecha de hoy.";


	public FechaDeInicioDeLicenciaFuturaException()
		: base(ERROR) { }

	public FechaDeInicioDeLicenciaFuturaException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public FechaDeInicioDeLicenciaFuturaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
