using Domain.Shared.Exceptions;

namespace Domain.Shared;

public class RangoFechas : ValueObject
{
	public DateTime FechaInicio { get; private set; }
	public DateTime? FechaFin { get; private set; }


	#region CONSTRUCTOR
	private RangoFechas(DateTime fechaInicio) =>
		FechaInicio = fechaInicio.Date;

	private RangoFechas(DateTime fechaInicio, DateTime? fechaFin)
		: this(fechaInicio)
	{
		if (fechaFin.HasValue && fechaInicio.Date > fechaFin.Value.Date)
		{
			throw new FechasInconsistentesException();
		}

		FechaFin = fechaFin;
	}

	public static RangoFechas Create(DateTime fechaInicio) =>
		new(fechaInicio);

	public static RangoFechas Create(DateTime fechaInicio, DateTime fechaFin) =>
		new(fechaInicio, fechaFin);
	#endregion

	public RangoFechas ActualizarFechaFin(DateTime fechaFin) =>
		new(FechaInicio, fechaFin);

	public bool HaIniciado() =>
		FechaInicio <= DateTime.Today.Date;

	public bool HaFinalizado() =>
		(FechaFin.HasValue && FechaFin.Value.Date < DateTime.Today.Date);

	public bool EstaVigente() =>
		HaIniciado() && !HaFinalizado();

	public bool EsIndeterminado() =>
		!FechaFin.HasValue;

	public double Duracion(Tiempo tiempo) => tiempo switch
	{
		Tiempo.Dia => EsIndeterminado() ? 0 : (FechaFin.Value - FechaInicio).TotalDays,
		Tiempo.Mes => EsIndeterminado() ? 0 : ((FechaFin.Value - FechaInicio).TotalDays) / 12,
		Tiempo.Año => EsIndeterminado() ? 0 : ((FechaFin.Value - FechaInicio).TotalDays) / 365,
		_ => throw new ArgumentException("Error al calcular la duración.")
	};

	public double TiempoTranscrurrido(Tiempo tiempo) => tiempo switch
	{
		Tiempo.Dia => HaFinalizado() ? (DateTime.Today - FechaInicio).TotalDays : (FechaFin.Value - FechaInicio).TotalDays,
		Tiempo.Mes => HaFinalizado() ? ((DateTime.Today - FechaInicio).TotalDays)/12 : ((FechaFin.Value - FechaInicio).TotalDays)/12,
		Tiempo.Año => HaFinalizado() ? ((DateTime.Today - FechaInicio).TotalDays)/365 : ((FechaFin.Value - FechaInicio).TotalDays)/365,
		_ => throw new ArgumentException("Error al calcular el tiempo transcurrido.")
	};

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return FechaInicio;
		yield return FechaFin;
	}
}