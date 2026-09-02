using Domain.Cursos.Exceptions;
using Domain.Shared;
using System.Text.RegularExpressions;

namespace Domain.Cursos;

public class Division : Entity
{
	private const int MAX_ALUMNOS = 35;
	private const int MIN_ALUMNOS = 15;

	private List<Guid> _cursantes = new();

	public string Descripcion { get; private set; } = string.Empty;
	public Guid? Preceptor { get; private set; } = null;
	public int TotalAlumnos => _cursantes.Count;
	public IReadOnlyCollection<Guid> Cursantes => _cursantes.ToList();


	#region CONSTRUCTOR
	private Division()
		: base() { }

	private Division(Guid divisionID)
		: base(divisionID) { }

	public Division(string descripcion)
		: this(Guid.NewGuid())
	{
		if (string.IsNullOrWhiteSpace(descripcion))
		{
			throw new ArgumentNullException(nameof(descripcion), "Datos incompletos del nombre de la división.");
		}

		if (!Regex.IsMatch(descripcion.Trim(), @"^([a-zA-Z]){1}$", RegexOptions.None))
		{
			throw new ArgumentException("La división del curso debe ser una letra.", nameof(descripcion));
		}

		Descripcion = descripcion.ToUpper().Trim();
	}
	#endregion

	#region Preceptor
	public bool EstaCargoPreceptorVacante() =>
		Preceptor is null;

	public void AsignarPreceptor(Guid unPreceptor)
	{
		if (!EstaCargoPreceptorVacante())
		{
			throw new CargoPreceptorNoDisponibleException();
		}

		if (Guid.Empty.Equals(unPreceptor))
		{
			throw new SinDatosPreceptorException();
		}

		Preceptor = unPreceptor;
	}

	public void QuitarPreceptor()
	{
		if (EstaCargoPreceptorVacante())
		{
			throw new CargoPreceptorDisponibleException();
		}

		Preceptor = null;
	}
	#endregion

	#region Cursantes
	public bool ExisteCursante(Guid unCursante)
	{
		if (Guid.Empty.Equals(unCursante))
		{
			throw new NullReferenceException($"Datos incompletos o inexistentes para comprobar si el alumno se encuentra registrado en la división.");
		}

		return _cursantes.Contains(unCursante);
	}

	private Guid BuscarCursante(Guid unCursante)
	{
		if (!ExisteCursante(unCursante))
		{
			throw new CursanteNoEncontradoException();
		}

		return _cursantes.Find(x => x.Equals(unCursante));
	}

	public void AgregarCursante(Guid unCursante)
	{
		if (ExisteCursante(unCursante))
		{
			throw new CursanteRegistradoException();
		}

		_cursantes.Add(unCursante);
	}

	public void QuitarCursante(Guid unCursante)
	{
		if (!ExisteCursante(unCursante))
		{
			throw new CursanteNoEncontradoException();
		}

		_cursantes.Remove(unCursante);
	}
	#endregion

	/*
	#region Calificaciones
	public IReadOnlyCollection<Calificacion> NotasDelAlumno(Guid unAlumno, string periodo)
	{
		var cicloLectivo = CicloLectivo.Crear(periodo);
		var cursante = _cursantes.Where(x => x.AlumnoID.Equals(unAlumno) && x.CicloLectivo.Equals(cicloLectivo))
										   .FirstOrDefault();
		if (cursante is null)
		{
			throw new ArgumentNullException("No se encuentra el alumno registrado en la división ni el ciclo lectivo seleccionado.");
		}

		return cursante.Calificaciones;
	}

	public void AgregarCalificacion(Guid unCursante, Guid unaMateria, bool asistencia, DateTime fecha, Instancia instancia, double? nota)
	{
		var cursante = BuscarCursante(unCursante);
		var nuevaCalificacion = Calificacion.Crear(unaMateria, asistencia, fecha, instancia, nota);
		cursante.AgregarCalificacion(nuevaCalificacion);
	}

	public void QuitarCalificacion(Guid unCursante, Calificacion aEliminar)
	{
		var cursante = BuscarCursante(unCursante);
		cursante.QuitarCalificacion(aEliminar);
	}
	#endregion
	*/
}