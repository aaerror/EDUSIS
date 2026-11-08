using Domain.Cursantes.Calificaciones.Exceptions;
using Domain.Shared;

namespace Domain.Cursantes.Calificaciones;

public sealed class Calificacion : Entity
{
	private const int MAX_OBSERVACION = 140;

	/// <summary>
	/// Calificación mínima de aprobación.
	/// </summary>
	/// <remarks>
	/// Pública porque la documentación que informa calificaciones —boletín y constancias de
	/// materias— tiene que declarar el umbral que aplicó, y tenerlo escrito también allá
	/// significaría dos números que pueden divergir: el que decide y el que se imprime.
	/// </remarks>
	public const double NOTA_APROBACION = 6;

	public Guid MateriaID { get; private set; }
	public DateTime Fecha { get; private set; }

	/// <summary>El tramo del ciclo lectivo en el que se tomó.</summary>
	public Periodo Periodo { get; private set; }

	/// <summary>
	/// Es el segundo intento dentro de <see cref="Periodo"/>, no un período aparte.
	/// </summary>
	/// <remarks>
	/// Está separado del período porque un ciclo tiene hasta cuatro recuperatorios —uno por
	/// cuatrimestre y uno por período de recuperación— y guardarlos todos como un mismo valor de
	/// <see cref="Calificaciones.Periodo"/> haría imposible saber a cuál de los cuatro pertenece
	/// cada nota. Quién le gana a quién lo decide <see cref="RendimientoAcademico"/>.
	/// </remarks>
	public bool EsRecuperatorio { get; private set; }

	public bool Rindio { get; private set; }
	public double? Nota { get; private set; }
	public string? Observacion { get; private set; } = string.Empty;


	#region CONSTRUCTOR
	private Calificacion() { }

	private Calificacion(Guid materiaID, DateTime fecha, Periodo periodo, bool esRecuperatorio, bool rindio, double? nota, string? observacion)
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

		if (esRecuperatorio && periodo is Periodo.Final)
		{
			throw new RecuperatorioDeNotaFinalException();
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
		Periodo = periodo;
		EsRecuperatorio = esRecuperatorio;
		Observacion = observacion;
	}

	internal static Calificacion Crear(Guid materiaID, DateTime fecha, Periodo periodo, bool esRecuperatorio, double? nota, string? observacion) =>
		new(materiaID, fecha, periodo, esRecuperatorio, true, nota, observacion);

	internal static Calificacion Inasistencia(Guid materiaID, DateTime fecha, Periodo periodo, bool esRecuperatorio, string? observacion) =>
		new(materiaID, fecha, periodo, esRecuperatorio, false, null, observacion);
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

	/// <summary>
	/// Esta calificación, por sí sola, alcanza el umbral de aprobación.
	/// </summary>
	/// <remarks>
	/// Habla de UNA instancia y no de la materia: un recuperatorio aprobado no aprueba la materia
	/// por sí mismo, ni un aplazo en el primer cuatrimestre la desaprueba. Eso lo decide
	/// <see cref="RendimientoAcademico.EstaAprobada"/> mirando todas las instancias juntas.
	/// </remarks>
	public bool EstaAprobado() =>
		Rindio && Nota >= NOTA_APROBACION;
}
