namespace Domain.Licencias.Exceptions;

/// <summary>
/// La licencia no está pendiente de aprobación, y la operación pedida sólo vale sobre una pendiente.
/// La encarnan <c>AprobarLicencia</c>, <c>RechazarLicencia</c>, <c>ModificarSolicitud</c> y
/// <c>ValidarSePuedeEliminar</c>.
/// </summary>
/// <remarks>
/// Es la única excepción de esta regla: ni el servicio ni el agregado la señalan con una excepción
/// del BCL.
/// </remarks>
public class LicenciaNoPendienteException : Exception
{
	private const string ERROR = "La licencia no se encuentra pendiente.";


	public LicenciaNoPendienteException()
		: base(ERROR) { }

	public LicenciaNoPendienteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }

	public LicenciaNoPendienteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
