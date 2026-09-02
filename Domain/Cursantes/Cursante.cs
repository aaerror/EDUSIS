using Domain.Cursantes.Asistencias;
using Domain.Cursantes.Exceptions;
using Domain.Shared;

namespace Domain.Cursantes;

public sealed class Cursante : Entity
{
	private List<Asistencia> _asistencias = new();

	public Guid AlumnoID { get; private set; } = Guid.Empty;
	public CicloLectivo CicloLectivo { get; private set; }
	public DateTime FechaInicio { get; private set; }
	public DateTime? FechaFin { get; private set; }
	public bool EsRecursante { get; private set; }
	public int Ausencias => _asistencias.Where(x => Falta.Ausencia.Equals(x.Falta)).Count();
	public int Inasistencias => _asistencias.Where(x => Falta.Inasistencia.Equals(x.Falta)).Count();
	public int Tardanzas => _asistencias.Where(x => Falta.Tardanza.Equals(x.Falta)).Count();
	public IReadOnlyCollection<Asistencia> Asistencias => _asistencias.AsReadOnly();



	#region CONSTRUCTOR
	private Cursante() {}

	private Cursante(Guid cursanteID)
		: base(cursanteID) {}

	public Cursante(Guid unAlumno, CicloLectivo cicloLectivo, DateTime fechaInicio, bool esRecursante = false)
		: this(Guid.NewGuid())
	{
		if (Guid.Empty.Equals(unAlumno) || cicloLectivo is null)
		{
			throw new NullReferenceException($"Datos del alumno incompletos o inexistentes para registrarlo en la division.");
		}

		AlumnoID = unAlumno;
		CicloLectivo = cicloLectivo;
		FechaInicio = fechaInicio;
		EsRecursante = esRecursante;
	}
	#endregion

	#region Asistencias
	public void RegistrarAsistencia(DateTime unaFecha, Falta unaFalta, TimeSpan? minutos, string observacion)
	{
		var existeAsistencia = _asistencias.Any(x => x.Fecha.Date.Equals(unaFecha.Date));
		if (existeAsistencia)
		{
			throw new AsistenciaRegistradaException();
		}

		switch (unaFalta)
		{
			case Falta.Ausencia:
				var ausencia = Asistencia.Ausencia(unaFecha, observacion);
				_asistencias.Add(ausencia);
				break;

			case Falta.Inasistencia:
				var inasistencia = Asistencia.Inasistencia(unaFecha, observacion);
				_asistencias.Add(inasistencia);
				break;

			case Falta.Tardanza:
				var tardanza = Asistencia.Tardanza(unaFecha, minutos, observacion);
				_asistencias.Add(tardanza);
				break;

			default:
				throw new InvalidOperationException();
		}
	}
	#endregion

	/*
		public void AgregarCalificacion(Calificacion nuevaCalificacion)
		{
			_calificaciones.Add(nuevaCalificacion);
		}

		public void QuitarCalificacion(Calificacion aEliminar)
		{
			_calificaciones.Remove(aEliminar);
		}
	*/
}