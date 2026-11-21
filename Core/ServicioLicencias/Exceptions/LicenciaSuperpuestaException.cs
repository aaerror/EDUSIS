namespace Core.ServicioLicencias.Exceptions;

/// <summary>
/// El docente ya tiene otra licencia en alguno de los días pedidos: sólo puede haber una por período.
/// </summary>
/// <remarks>
/// Vive en <c>Core</c> y no en el dominio porque compara la licencia con las demás del docente, que
/// el agregado no ve; es el mismo molde que las reglas de unicidad.
/// </remarks>
public class LicenciaSuperpuestaException : Exception
{
	private const string ERROR = "El docente ya tiene otra licencia en ese período. Una licencia nueva puede empezar recién al día siguiente del último día de la anterior.";


	public LicenciaSuperpuestaException()
		: base(ERROR) {}
	public LicenciaSuperpuestaException(string parametro)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }")) {}
	public LicenciaSuperpuestaException(string parametro, Exception exception)
		: base(string.Format($"{ ERROR }. Parámetro: { parametro }"), exception) {}
}
