namespace Domain.Licencias.Exceptions;

public class PlazoFijoDeLicenciaIncumplidoException : Exception
{
	private const string ERROR = "La cantidad de días no coincide con la que fija la norma para este tipo de licencia.";


	public PlazoFijoDeLicenciaIncumplidoException() : base(ERROR) { }
	public PlazoFijoDeLicenciaIncumplidoException(PlazoLegal plazo)
		: base($"{ERROR} {plazo.Denominacion} ({plazo.Norma}) corresponde a {plazo.Dias} días.") { }
	public PlazoFijoDeLicenciaIncumplidoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PlazoFijoDeLicenciaIncumplidoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
