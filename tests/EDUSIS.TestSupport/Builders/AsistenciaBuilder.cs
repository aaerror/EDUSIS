using Domain.Cursantes.Asistencias;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder del value object <see cref="Asistencia"/>. Estado por defecto válido: una
/// <see cref="Falta.Ausencia"/> con fecha de hoy. <c>Falta</c> es un <c>enum</c> del dominio y
/// no necesita builder propio. <see cref="Build"/> usa las factorías públicas de
/// <see cref="Asistencia"/>; sólo <see cref="Falta.Tardanza"/> lleva minutos.
/// </summary>
public sealed class AsistenciaBuilder
{
	#region ESTADO POR DEFECTO
	private DateTime _fecha = DateTime.Today;
	private Falta _falta = Falta.Ausencia;
	private TimeSpan? _minutos = null;
	private string _observacion = "Sin aviso";
	#endregion

	#region CONFIGURACIÓN
	public AsistenciaBuilder ConFecha(DateTime fecha)
	{
		_fecha = fecha;
		return this;
	}

	public AsistenciaBuilder ConFalta(Falta falta)
	{
		_falta = falta;
		return this;
	}

	public AsistenciaBuilder ComoAusencia()
	{
		_falta = Falta.Ausencia;
		_minutos = null;
		return this;
	}

	public AsistenciaBuilder ComoInasistencia()
	{
		_falta = Falta.Inasistencia;
		_minutos = null;
		return this;
	}

	public AsistenciaBuilder ComoTardanza(TimeSpan minutos)
	{
		_falta = Falta.Tardanza;
		_minutos = minutos;
		return this;
	}

	public AsistenciaBuilder ConObservacion(string observacion)
	{
		_observacion = observacion;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Asistencia Build() => _falta switch
	{
		Falta.Ausencia => Asistencia.Ausencia(_fecha, _observacion),
		Falta.Inasistencia => Asistencia.Inasistencia(_fecha, _observacion),
		Falta.Tardanza => Asistencia.Tardanza(_fecha, _minutos, _observacion),
		_ => Asistencia.Crear(_fecha, _minutos, _falta, _observacion),
	};
	#endregion
}
