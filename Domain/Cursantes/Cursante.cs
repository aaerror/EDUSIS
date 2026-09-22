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
		AlumnoID = unAlumno;
		CicloLectivo = cicloLectivo;
		FechaInicio = fechaInicio;
		EsRecursante = esRecursante;
	}
	#endregion

	#region Calificaciones
	public Guid RegistrarCalificacion(Guid materiaID, DateTime fecha, Instancia instancia, double? nota, string? observacion)
	{
		var calificacion = Calificacion.Crear(materiaID, fecha, instancia, nota, observacion);
		_calificaciones.Add(calificacion);

		return calificacion.Id;
	}

	public Guid RegistrarInasistenciaAExamen(Guid materiaID, DateTime fecha, Instancia instancia, string? observacion)
	{
		var calificacion = Calificacion.Inasistencia(materiaID, fecha, instancia, observacion);
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