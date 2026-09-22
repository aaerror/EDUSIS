using Domain.Asistencias.Exceptions;
using Domain.Shared;

namespace Domain.Asistencias;

public sealed class RegistroAsistencia : Entity
{
	public Guid CursanteID { get; private set; }
	public TipoAsistencia Tipo { get; private set; } = TipoAsistencia.Presente;
	public TimeSpan? Minutos { get; private set; }
	public string? Observacion { get; private set; }


	#region CONSTRUCTOR
	private RegistroAsistencia() { }

	internal RegistroAsistencia(Guid cursanteID)
		: base(Guid.NewGuid())
	{
		CursanteID = cursanteID;
		Tipo = TipoAsistencia.Presente;
		Minutos = null;
		Observacion = null;
	}
	#endregion

	#region Mutadores
	internal void MarcarPresente()
	{
		Tipo = TipoAsistencia.Presente;
		Minutos = null;
		Observacion = null;
	}

	internal void MarcarAusencia(string? observacion = null)
	{
		ValidarObservacion(observacion);
		Tipo = TipoAsistencia.Ausencia;
		Minutos = null;
		Observacion = observacion;
	}

	internal void MarcarInasistencia(string? observacion = null)
	{
		ValidarObservacion(observacion);
		Tipo = TipoAsistencia.Inasistencia;
		Minutos = null;
		Observacion = observacion;
	}

	internal void MarcarTardanza(TimeSpan minutos, string? observacion = null)
	{
		if (minutos <= TimeSpan.Zero)
		{
			throw new TardanzaSinMinutosException();
		}

		ValidarObservacion(observacion);
		Tipo = TipoAsistencia.Tardanza;
		Minutos = minutos;
		Observacion = observacion;
	}

	private static void ValidarObservacion(string? observacion)
	{
		if (observacion != null && observacion.Length > 140)
		{
			throw new ObservacionExtensaException();
		}
	}
	#endregion
}
