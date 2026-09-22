using Domain.Asistencias;

namespace EDUSIS.TestSupport.Builders;

/// <summary>
/// Builder de <see cref="PlanillaAsistencia"/>. Estado por defecto válido: división, fecha
/// (hoy), preceptor y cursantes (uno) todos con Guid.NewGuid(), abierta.
/// </summary>
public sealed class PlanillaAsistenciaBuilder
{
	#region ESTADO POR DEFECTO
	private Guid _divisionID = Guid.NewGuid();
	private DateTime _fecha = DateTime.Today;
	private Guid _preceptorID = Guid.NewGuid();
	private readonly List<Guid> _cursantesIDs = new() { Guid.NewGuid() };
	private bool _cerrada = false;
	#endregion

	#region CONFIGURACIÓN
	public PlanillaAsistenciaBuilder ConDivision(Guid divisionID)
	{
		_divisionID = divisionID;
		return this;
	}

	public PlanillaAsistenciaBuilder ConFecha(DateTime fecha)
	{
		_fecha = fecha;
		return this;
	}

	public PlanillaAsistenciaBuilder ConPreceptor(Guid preceptorID)
	{
		_preceptorID = preceptorID;
		return this;
	}

	public PlanillaAsistenciaBuilder ConCursantes(params Guid[] cursantesIDs)
	{
		_cursantesIDs.Clear();
		_cursantesIDs.AddRange(cursantesIDs);
		return this;
	}

	public PlanillaAsistenciaBuilder Cerrada()
	{
		_cerrada = true;
		return this;
	}
	#endregion

	#region CONSTRUCCIÓN
	public PlanillaAsistencia Build()
	{
		var planilla = PlanillaAsistencia.Abrir(_divisionID, _fecha, _preceptorID, _cursantesIDs);

		if (_cerrada)
		{
			planilla.Cerrar();
		}

		return planilla;
	}
	#endregion
}
