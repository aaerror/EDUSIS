using Domain.Cursantes.Calificaciones.Exceptions;
using Domain.Shared;

namespace Domain.Cursantes.Calificaciones;

public sealed class Calificacion : Entity
{
	private const int MAX_OBSERVACION = 140;
	private const double NOTA_APROBACION = 6;

	public Guid MateriaID { get; private set; }
	public DateTime Fecha { get; private set; }
	public Instancia Instancia { get; private set; }
	public bool Rindio { get; private set; }
	public double? Nota { get; private set; }
	public string? Observacion { get; private set; } = string.Empty;


	#region CONSTRUCTOR
	private Calificacion() { }

	private Calificacion(Guid materiaID, DateTime fecha, Instancia instancia, bool rindio, double? nota, string? observacion)
		: base(Guid.NewGuid())
	{
		if (Guid.Empty.Equals(materiaID))
		{
			throw new SinDatosMateriaException();
		}

		if (fecha.Date > DateTime.Today.Date)
		{
			throw new ArgumentException("La fecha del exámen no puede ser posterior a la fecha de registro en sistema.", nameof(fecha.Date));
		}

		if (rindio)
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

		ValidarObservacion(observacion);

		MateriaID = materiaID;
		Fecha = fecha.Date;
		Rindio = rindio;
		Instancia = instancia;
		Observacion = observacion;
	}

	internal static Calificacion Crear(Guid materiaID, DateTime fecha, Instancia instancia, double? nota, string? observacion) =>
		new(materiaID, fecha, instancia, true, nota, observacion);

	internal static Calificacion Inasistencia(Guid materiaID, DateTime fecha, Instancia instancia, string? observacion) =>
		new(materiaID, fecha, instancia, false, null, observacion);
	#endregion

	#region Observación
	internal void ModificarObservacion(string? observacion)
	{
		ValidarObservacion(observacion);

		Observacion = observacion;
	}

	private static void ValidarObservacion(string? observacion)
	{
		if (observacion?.Length > MAX_OBSERVACION)
		{
			throw new ArgumentException("El detalle de observación es demasiado largo.");
		}
	}
	#endregion

	public bool EstaAprobado() =>
		Rindio && Nota >= NOTA_APROBACION;
}
