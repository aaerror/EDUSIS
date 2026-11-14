namespace Domain.Licencias.Exceptions;

/// <summary>
/// La licencia no está pendiente de aprobación, y la operación pedida sólo vale sobre una pendiente.
/// La encarnan <c>AprobarLicencia</c>, <c>CancelarLicencia</c> y <c>ValidarSePuedeEliminar</c>.
/// </summary>
/// <remarks>
/// Hasta el 2026-11-14 esta regla se señalaba con <c>ArgumentException("La licencia no se encuentra
/// pendiente.")</c> en los dos primeros métodos, y el tercero no existía: la baja de una licencia
/// validaba el estado desde <c>ServicioLicencia</c>, leyendo <c>licencia.Estado</c> y decidiendo por su
/// cuenta con otra excepción del BCL. Era CORE-22.
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
