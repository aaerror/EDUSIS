using Domain.Shared;

namespace Domain.Cursantes.Asistencias;

public class Asistencia : ValueObject
{
	public DateTime Fecha { get; private set; }
	public TimeSpan? Minutos { get; private set; }
	public Falta Falta { get; private set; }
	public string Observacion { get; private set; } = string.Empty;


	#region CONSTRUCTOR
	private Asistencia() { }

	private Asistencia(DateTime unaFecha, Falta unaFalta, string unaObservacion)
	{
		Fecha = unaFecha.Date;
		Falta = unaFalta;
		Observacion = unaObservacion;
	}

	private Asistencia(DateTime unaFecha, TimeSpan? minutos, Falta unaFalta, string unaObservacion)
		: this(unaFecha, unaFalta, unaObservacion)
	{
		Minutos = minutos;
	}

	public static Asistencia Crear(DateTime unaFecha, TimeSpan? minutos, Falta unaFalta, string unaObservacion) =>
		new Asistencia(unaFecha, minutos, unaFalta, unaObservacion);

	public static Asistencia Ausencia(DateTime unaFecha, string unaObservacion) =>
		new Asistencia(unaFecha, Falta.Ausencia, unaObservacion);

	public static Asistencia Inasistencia(DateTime unaFecha, string unaObservacion) =>
		new Asistencia(unaFecha, Falta.Inasistencia, unaObservacion);

	public static Asistencia Tardanza(DateTime unaFecha, TimeSpan? minutos, string unaObservacion) =>
		new Asistencia(unaFecha, minutos, Falta.Tardanza, unaObservacion);
	#endregion

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return Fecha;
		yield return Minutos;
		yield return Falta;
		yield return Observacion;
	}
}