using Domain.Curriculas.Exceptions;
using Domain.Shared;

namespace Domain.Curriculas.Materias;

public class Calificacion : ValueObject
{
	public Guid CursanteID { get; private set; }
	public DateTime Fecha { get; private set; }
	public Instancia Instancia { get; private set; }
	public bool Asistencia { get; private set; }
	public double? Nota { get; private set; }
	public string? Observacion { get; private set; } = string.Empty;


	#region CONSTRUCTOR
	private Calificacion() { }

	private Calificacion(Guid cursanteID, DateTime fecha, Instancia instancia, bool asistencia, double? nota, string? observacion)
	{
		if (fecha.Date > DateTime.Today.Date)
		{
			throw new ArgumentException("La fecha del exámen no puede ser posterior a la fecha de registro en sistema.", nameof(fecha.Date));
		}

		if (asistencia)
		{
			if (nota is null)
			{
				throw new ArgumentNullException(nameof(nota), "Debe especificar la nota del alumno para registrar una calificación.");
			}

			if (nota < 1 || nota > 10)
			{
				throw new NotaIncorrectaException();
			}

			Nota = Math.Round((double)nota, 2);
		}

		if (observacion?.Length > 140)
		{
			throw new ArgumentException("El detalle de observación es demasiado largo.");
		}

		CursanteID = cursanteID;
		Fecha = fecha.Date;
		Asistencia = asistencia;
		Instancia = instancia;
		Observacion = observacion;
	}

	public static Calificacion Crear(Guid cursanteID, DateTime fecha, Instancia instancia, double? nota, string? observacion) =>
		new(cursanteID, fecha, instancia, true, nota, observacion);
	#endregion

	public static Calificacion Inasistencia(Guid cursanteID, DateTime fecha, Instancia instancia, string? observacion) =>
		new(cursanteID, fecha, instancia, false, null, observacion);

	public Calificacion ModificarObservaciones(string? observacion) =>
		new(CursanteID, Fecha, Instancia, Asistencia, Nota, observacion);

	public bool EstaAprobado() =>
		Asistencia ? Nota > 5 : false;

	public override IEnumerable<object> GetEqualityCommponents()
	{
		yield return CursanteID;
		yield return Fecha;
		yield return Instancia;
		yield return Asistencia;
		yield return Nota;
		yield return Observacion;
	}
}