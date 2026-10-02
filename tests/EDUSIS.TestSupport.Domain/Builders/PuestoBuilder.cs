using System.Linq;
using Domain.Docentes;
using Domain.Docentes.Puestos;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="Puesto"/> (cargo docente). El constructor y los mutadores de
/// <see cref="Puesto"/> son <c>internal</c> — sólo <see cref="Docente"/> instancia y modifica
/// sus puestos —, así que este builder arma un <see cref="Docente"/> auxiliar con
/// <see cref="DocenteBuilder"/> y le asigna el cargo. Estado por defecto válido: posición
/// <c>Profesor</c>, estado <c>Pendiente</c>, inicio hoy, sin fecha de fin.
/// </summary>
public sealed class PuestoBuilder
{
	#region ESTADO POR DEFECTO
	private string _estado = "Pendiente";
	private string _posicion = "Profesor";
	private DateTime _desde = DateTime.Today;
	private DateTime? _hasta = null;
	#endregion

	#region CONFIGURACIÓN
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
	/// <summary>
	/// Docente auxiliar con el puesto configurado como único cargo. Para ejercitar el ciclo de
	/// vida del puesto a través de la raíz.
	/// </summary>
	public Docente BuildDocente() =>
		new DocenteBuilder().ConPuesto(_posicion, _estado, _desde, _hasta).Build();

	public Puesto Build() =>
		BuildDocente().Puestos.Single();
	#endregion
}
