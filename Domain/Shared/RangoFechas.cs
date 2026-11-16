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
		FechaInicio <= Reloj.Hoy;

	public bool HaFinalizado() =>
		(FechaFin.HasValue && FechaFin.Value.Date < Reloj.Hoy);

	public bool EstaVigente() =>
		HaIniciado() && !HaFinalizado();

	public bool EsIndeterminado() =>
		!FechaFin.HasValue;

	/// <summary>
	/// Extensión total del rango. Un rango indeterminado no tiene duración y devuelve 0.
	/// </summary>
	public double Duracion(Tiempo tiempo) =>
		EsIndeterminado() ? 0 : Medir(FechaInicio, FechaFin!.Value.Date, tiempo);

	/// <summary>
	/// Lo recorrido del rango hasta hoy: 0 si no inició, hasta <see cref="FechaFin"/> si ya
	/// finalizó y hasta hoy si sigue en curso (también cuando es indeterminado).
	/// </summary>
	public double TiempoTranscurrido(Tiempo tiempo)
	{
		if (!HaIniciado())
		{
			return 0;
		}

		var hasta = HaFinalizado() ? FechaFin!.Value.Date : Reloj.Hoy;

		return Medir(FechaInicio, hasta, tiempo);
	}

	/// <summary>
	/// Única fuente de la conversión de un intervalo a días, meses o años; <paramref name="desde"/>
	/// nunca es posterior a <paramref name="hasta"/> (lo garantizan el constructor y los llamadores).
	/// </summary>
	/// <remarks>
	/// Los días son la diferencia (el día de fin no se suma), como se medía antes. Meses y años son
	/// calendario: del 15/01 al 15/03 son 2 meses exactos, y el resto se expresa como fracción del
	/// mes en que cae. Dividir días por 30 o por 365 desplaza el resultado según los meses y los
	/// años bisiestos que atraviese el rango.
	/// </remarks>
	private static double Medir(DateTime desde, DateTime hasta, Tiempo tiempo) => tiempo switch
	{
		Tiempo.Dia => (hasta - desde).TotalDays,
		Tiempo.Mes => MesesCalendario(desde, hasta),
		Tiempo.Año => MesesCalendario(desde, hasta) / 12,
		_ => throw new ArgumentOutOfRangeException(nameof(tiempo), "La unidad de tiempo indicada no existe.")
	};

	private static double MesesCalendario(DateTime desde, DateTime hasta)
	{
		var completos = (hasta.Year - desde.Year) * 12 + hasta.Month - desde.Month;

		// AddMonths ajusta al último día del mes cuando el día no existe (31/01 + 1 mes = 28/02),
		// así que el corte se compara contra la fecha, no contra el número de día.
		if (desde.AddMonths(completos) > hasta)
		{
			completos--;
		}

		var corte = desde.AddMonths(completos);
		var siguiente = desde.AddMonths(completos + 1);

		return completos + (hasta - corte).TotalDays / (siguiente - corte).TotalDays;
	}

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return FechaInicio;
		yield return FechaFin;
	}
}