using Domain.Licencias;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Licencia"/>. Estado por defecto válido: artículo <c>Enfermedad</c>,
/// inicio hoy, sin fecha de fin, estado <c>Pendiente</c>. La <c>Licencia</c> resultante llega
/// con el evento <c>LicenciaSolicitadaEvent</c> encolado, como en producción.
/// </summary>
public sealed class LicenciaBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _docenteID = Guid.NewGuid();
	private string _articulo = "Enfermedad";
	private DateTime _fechaInicio = DateTime.Today;
	private DateTime? _fechaFin = null;
	private string? _observacion = "Reposo por indicación médica";
	private bool _aprobar = false;
	#endregion

	#region CONFIGURACIÓN
	public LicenciaBuilder ConDocente(Guid docenteID)
	{
		_docenteID = docenteID;
		return this;
	}

	public LicenciaBuilder ConArticulo(string articulo)
	{
		_articulo = articulo;
		return this;
	}

	public LicenciaBuilder ConFechaInicio(DateTime fechaInicio)
	{
		_fechaInicio = fechaInicio;
		return this;
	}

	public LicenciaBuilder ConFechaFin(DateTime? fechaFin)
	{
		_fechaFin = fechaFin;
		return this;
	}

	public LicenciaBuilder ConObservacion(string? observacion)
	{
		_observacion = observacion;
		return this;
	}

	/// <summary>Aprueba la licencia tras construirla (pasa de <c>Pendiente</c> a <c>Activa</c>).</summary>
	public LicenciaBuilder Aprobada()
	{
		_aprobar = true;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Licencia Build()
	{
		var licencia = _fechaFin.HasValue
			? new Licencia(_docenteID, _articulo, _fechaInicio, _fechaFin.Value, _observacion)
			: new Licencia(_docenteID, _articulo, _fechaInicio, _observacion);

		if (_aprobar)
		{
			licencia.AprobarLicencia(_observacion);
		}

		return licencia;
	}
	#endregion
}
