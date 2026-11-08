using Domain.Cursantes.Calificaciones;
using Domain.Cursantes.Exceptions;
using Domain.Shared;

namespace Domain.Cursantes;

public sealed class Cursante : Entity
{
	private readonly List<Calificacion> _calificaciones = new();

	public Guid DivisionID { get; private set; } = Guid.Empty;
	public Guid AlumnoID { get; private set; } = Guid.Empty;
	public CicloLectivo CicloLectivo { get; private set; }
	public DateTime FechaInicio { get; private set; }
	public DateTime? FechaFin { get; private set; }
	public bool EsRecursante { get; private set; }
	public IReadOnlyCollection<Calificacion> Calificaciones => _calificaciones.AsReadOnly();



	#region CONSTRUCTOR
	private Cursante() {}

	private Cursante(Guid cursanteID)
		: base(cursanteID) {}

	public Cursante(Guid unaDivision, Guid unAlumno, CicloLectivo cicloLectivo, DateTime fechaInicio, bool esRecursante = false)
		: this(Guid.NewGuid())
	{
		if (Guid.Empty.Equals(unaDivision))
		{
			throw new SinDatosDivisionException();
		}

		if (Guid.Empty.Equals(unAlumno) || cicloLectivo is null)
		{
			throw new SinDatosAlumnoException();
		}

		DivisionID = unaDivision;

		// Copia propia del ciclo lectivo, NO la instancia que vino del llamador. Cierra INFRA-18.
		//
		// CicloLectivo se mapea con OwnsOne (CursantesConfiguration), y una entidad propiedad de EF
		// pertenece a exactamente UN dueño: si dos Cursante comparten la misma instancia, EF le
		// asigna el owner a uno y al otro le escribe NULL en ciclo_lectivo — que es NOT NULL, así que
		// el INSERT revienta, y si la columna fuera anulable escribiría NULL en silencio. El defecto
		// lo encontró el seeder (`var ciclo = ...` reutilizado en un bucle de inscripciones) y lo
		// reproduce ContarCursantesDeDivisionAsync_devuelve_cantidad_de_cursantes_activos.
		//
		// La copia va acá y no en cada llamador porque es el agregado el que no puede compartir su
		// value object: pedirle a los veinte sitios que inscriben que construyan un ciclo nuevo cada
		// vez es una regla que nadie va a recordar, y el que la olvide no se entera hasta que inserta
		// el segundo cursante. La igualdad de ValueObject es estructural, así que la copia es igual
		// al original y ningún consumidor nota la diferencia.
		AlumnoID = unAlumno;
		CicloLectivo = CicloLectivo.Crear(cicloLectivo.Periodo);
		FechaInicio = fechaInicio;
		EsRecursante = esRecursante;
	}
	#endregion

	#region Inscripción
	/// <summary>
	/// La inscripción está activa mientras no tenga fecha de fin. Una cerrada es registro
	/// histórico: queda en el historial del alumno y no admite cambios.
	/// </summary>
	public bool EstaActiva() => FechaFin is null;

	public void ModificarCondicionDeRecursante(bool esRecursante)
	{
		if (!EstaActiva())
		{
			throw new InscripcionCerradaException();
		}

		EsRecursante = esRecursante;
	}

	/// <summary>
	/// Cierra la inscripción con la fecha de baja indicada. Es la única vía que escribe
	/// <see cref="FechaFin"/>: el traslado de división la usa para cerrar la inscripción de origen
	/// antes de abrir la de destino.
	/// </summary>
	/// <remarks>
	/// Una regla, una excepción: la baja de una inscripción ya cerrada, la fecha anterior al inicio
	/// de la cursada y la fecha futura son tres errores distintos y el usuario tiene que poder
	/// distinguirlos en el mensaje. El criterio de "futura" es el mismo que usa
	/// <c>PlanillaAsistencia</c>: no se registra lo que todavía no pasó.
	/// </remarks>
	public void DarDeBaja(DateTime fechaBaja)
	{
		if (!EstaActiva())
		{
			throw new InscripcionCerradaException();
		}

		if (fechaBaja.Date < FechaInicio.Date)
		{
			throw new FechaBajaAnteriorAlInicioException();
		}

		if (fechaBaja.Date > DateTime.Today)
		{
			throw new FechaBajaFuturaException();
		}

		FechaFin = fechaBaja;
	}
	#endregion

	#region Calificaciones
	public Guid RegistrarCalificacion(Guid materiaID, DateTime fecha, Periodo periodo, bool esRecuperatorio, double? nota, string? observacion)
	{
		var calificacion = Calificacion.Crear(materiaID, fecha, periodo, esRecuperatorio, nota, observacion);
		_calificaciones.Add(calificacion);

		return calificacion.Id;
	}

	public Guid RegistrarInasistenciaAExamen(Guid materiaID, DateTime fecha, Periodo periodo, bool esRecuperatorio, string? observacion)
	{
		var calificacion = Calificacion.Inasistencia(materiaID, fecha, periodo, esRecuperatorio, observacion);
		_calificaciones.Add(calificacion);

		return calificacion.Id;
	}

	public void QuitarCalificacion(Guid calificacionID)
	{
		var calificacion = BuscarCalificacion(calificacionID);

		_calificaciones.Remove(calificacion);
	}

	public void ModificarObservacionCalificacion(Guid calificacionID, string? observacion)
	{
		var calificacion = BuscarCalificacion(calificacionID);

		calificacion.ModificarObservacion(observacion);
	}

	private Calificacion BuscarCalificacion(Guid calificacionID) =>
		_calificaciones.FirstOrDefault(x => x.Id.Equals(calificacionID))
			?? throw new CalificacionNoEncontradaException();
	#endregion
}