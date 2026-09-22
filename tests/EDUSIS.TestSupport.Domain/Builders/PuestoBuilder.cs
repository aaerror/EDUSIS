using Domain.Docentes.Puestos;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Puesto"/> suelto (sin pasar por <see cref="Domain.Docentes.Docente"/>),
/// para pruebas que sólo ejercitan el ciclo de vida del puesto. Estado por defecto válido:
/// posición <c>Profesor</c>, estado <c>Pendiente</c>, inicio hoy, sin fecha de fin.
/// </summary>
public sealed class PuestoBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _docenteID = Guid.NewGuid();
	private string _estado = "Pendiente";
	private string _posicion = "Profesor";
	private DateTime _desde = DateTime.Today;
	private DateTime? _hasta = null;
	#endregion

	#region CONFIGURACIÓN
	public PuestoBuilder ConDocente(Guid docenteID)
	{
		_docenteID = docenteID;
		return this;
	}

	public PuestoBuilder ConEstado(string estado)
	{
		_estado = estado;
		return this;
	}

	public PuestoBuilder ConPosicion(string posicion)
	{
		_posicion = posicion;
		return this;
	}

	public PuestoBuilder ConDesde(DateTime desde)
	{
		_desde = desde;
		return this;
	}

	public PuestoBuilder ConHasta(DateTime? hasta)
	{
		_hasta = hasta;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public Puesto Build() =>
		new(_docenteID, _estado, _posicion, _desde, _hasta);
	#endregion
}
