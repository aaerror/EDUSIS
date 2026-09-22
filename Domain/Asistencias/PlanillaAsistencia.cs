using Domain.Asistencias.DomainEvents;
using Domain.Asistencias.Exceptions;
using Domain.Shared;

namespace Domain.Asistencias;

public sealed class PlanillaAsistencia : Entity
{
	public Guid DivisionID { get; private set; }
	public DateTime Fecha { get; private set; }
	public Guid PreceptorID { get; private set; }
	public bool Cerrada { get; private set; }
	private List<RegistroAsistencia> _registros = new();
	public IReadOnlyCollection<RegistroAsistencia> Registros => _registros.AsReadOnly();


	#region CONSTRUCTOR
	private PlanillaAsistencia() { }

	private PlanillaAsistencia(Guid planillaID)
		: base(planillaID) { }

	private PlanillaAsistencia(
		Guid planillaID,
		Guid divisionID,
		DateTime fecha,
		Guid preceptorID,
		IEnumerable<Guid> cursantesIDs)
			: this(planillaID)
	{
		if (Guid.Empty.Equals(divisionID))
		{
			throw new SinDatosDivisionException();
		}

		if (Guid.Empty.Equals(preceptorID))
		{
			throw new SinDatosPreceptorException();
		}

		if (fecha.Date > DateTime.Today)
		{
			throw new FechaPlanillaFuturaException();
		}

		var cursantesUnicos = cursantesIDs.Distinct().ToList();

		foreach (var cursanteID in cursantesUnicos)
		{
			if (Guid.Empty.Equals(cursanteID))
			{
				throw new SinDatosCursanteException();
			}
		}

		if (!cursantesUnicos.Any())
		{
			throw new PlanillaSinCursantesException();
		}

		DivisionID = divisionID;
		Fecha = fecha.Date;
		PreceptorID = preceptorID;
		Cerrada = false;
		_registros = cursantesUnicos.Select(c => new RegistroAsistencia(c)).ToList();
	}
	#endregion

	#region Factory
	public static PlanillaAsistencia Abrir(Guid divisionID, DateTime fecha, Guid preceptorID, IEnumerable<Guid> cursantesIDs)
	{
		return new PlanillaAsistencia(Guid.NewGuid(), divisionID, fecha, preceptorID, cursantesIDs);
	}
	#endregion

	#region Marcado de asistencias
	public void MarcarPresente(Guid cursanteID)
	{
		ValidarPlanillaAbierta();
		var registro = BuscarRegistroDeCursante(cursanteID);
		registro.MarcarPresente();
	}

	public void MarcarAusencia(Guid cursanteID, string? observacion = null)
	{
		ValidarPlanillaAbierta();
		var registro = BuscarRegistroDeCursante(cursanteID);
		registro.MarcarAusencia(observacion);
	}

	public void MarcarInasistencia(Guid cursanteID, string? observacion = null)
	{
		ValidarPlanillaAbierta();
		var registro = BuscarRegistroDeCursante(cursanteID);
		registro.MarcarInasistencia(observacion);
	}

	public void MarcarTardanza(Guid cursanteID, TimeSpan minutos, string? observacion = null)
	{
		ValidarPlanillaAbierta();
		var registro = BuscarRegistroDeCursante(cursanteID);
		registro.MarcarTardanza(minutos, observacion);
	}
	#endregion

	#region Incorporar cursante
	public void IncorporarCursante(Guid cursanteID)
	{
		if (Cerrada)
		{
			throw new PlanillaCerradaException();
		}

		if (Guid.Empty.Equals(cursanteID))
		{
			throw new SinDatosCursanteException();
		}

		if (_registros.Any(r => r.CursanteID.Equals(cursanteID)))
		{
			throw new CursanteYaIncluidoException();
		}

		_registros.Add(new RegistroAsistencia(cursanteID));
	}
	#endregion

	#region Ciclo (cerrar/reabrir)
	public void Cerrar()
	{
		if (Cerrada)
		{
			throw new PlanillaCerradaException();
		}

		Cerrada = true;
		AgregarEvento(new PlanillaAsistenciaCerradaEvent(Id, DivisionID, Fecha));
	}

	public void Reabrir()
	{
		if (!Cerrada)
		{
			throw new PlanillaAbiertaException();
		}

		Cerrada = false;
		AgregarEvento(new PlanillaAsistenciaReabiertaEvent(Id, DivisionID, Fecha));
	}
	#endregion

	#region Consultas
	public int CantidadCon(TipoAsistencia tipo)
	{
		return _registros.Count(r => r.Tipo.Equals(tipo));
	}

	public RegistroAsistencia? RegistroDe(Guid cursanteID)
	{
		return _registros.FirstOrDefault(r => r.CursanteID.Equals(cursanteID));
	}
	#endregion

	#region Validaciones
	private void ValidarPlanillaAbierta()
	{
		if (Cerrada)
		{
			throw new PlanillaCerradaException();
		}
	}

	private RegistroAsistencia BuscarRegistroDeCursante(Guid cursanteID)
	{
		var registro = RegistroDe(cursanteID);
		if (registro == null)
		{
			throw new CursanteNoIncluidoException();
		}

		return registro;
	}
	#endregion
}
