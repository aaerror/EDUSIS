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

	/// <summary>
	/// Borra el registro de asistencia de un cursante. Es la contraparte de
	/// <see cref="IncorporarCursante"/> para el borrado físico de la persona, y <strong>no</strong>
	/// para sacar a alguien de la lista del día.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>No mira <see cref="Cerrada"/>, y es deliberado.</strong> Es la única operación de este
	/// agregado que se salta esa guarda. El resto del marcado pasa por <c>ValidarPlanillaAbierta</c> y
	/// <see cref="IncorporarCursante"/> rechaza con <c>PlanillaCerradaException</c>, porque ahí la
	/// planilla cerrada es registro histórico y la salida correcta es reabrirla. Acá no: lo que se está
	/// borrando es el cursante, no corrigiendo el día. Si esto respetara <see cref="Cerrada"/>, un
	/// alumno con un solo día de asistencia en una planilla cerrada sería indestructible, y la clave
	/// ajena <c>FK_REGISTRO_ASISTENCIA_CURSANTE</c> —que es <c>Restrict</c>— haría fallar el borrado
	/// del alumno con una violación de integridad de SQL Server en lugar de con una regla de negocio.
	/// </para>
	/// <para>
	/// La planilla puede quedar sin ningún registro si el cursante borrado era el último. Este agregado
	/// no decide qué hacer con eso: el caso de uso que orquesta el borrado mira
	/// <see cref="Registros"/> y elimina la planilla vacía, porque una planilla sin registros no
	/// documenta ninguna jornada. La invariante de <c>PlanillaSinCursantesException</c> gobierna el
	/// <em>nacimiento</em> de la planilla, no su final.
	/// </para>
	/// </remarks>
	public void EliminarRegistroDe(Guid cursanteID)
	{
		if (Guid.Empty.Equals(cursanteID))
		{
			throw new SinDatosCursanteException();
		}

		var registro = _registros.FirstOrDefault(r => r.CursanteID.Equals(cursanteID));
		if (registro is null)
		{
			throw new CursanteNoIncluidoException();
		}

		_registros.Remove(registro);
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
	/// <summary>
	/// Habilita el borrado de la planilla sólo mientras está abierta.
	/// </summary>
	/// <remarks>
	/// Una planilla cerrada es registro histórico de lo que pasó ese día en esa división: se puede
	/// reabrir para corregirla (<see cref="Reabrir"/>), pero no se borra. Por eso la regla tiene su
	/// propia excepción, <c>PlanillaCerradaNoEliminableException</c>, y no reusa
	/// <c>PlanillaCerradaException</c>: esa queda reservada para el marcado de asistencia, donde el
	/// mensaje correcto es "la planilla está cerrada" y la salida es reabrirla. Acá el mensaje no es
	/// "reabrila" sino "no se borra", y la UI tiene que poder distinguir los dos casos.
	/// </remarks>
	public void ValidarSePuedeEliminar()
	{
		if (Cerrada)
		{
			throw new PlanillaCerradaNoEliminableException();
		}
	}

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
