using Domain.Shared;
using Domain.Curriculas.DomainEvents;
using Domain.Curriculas.Exceptions;
using Domain.Curriculas.Materias.CargosDocentes;

namespace Domain.Curriculas.Materias;

public sealed class Materia : Entity
{
	private readonly List<SituacionRevista> _docentes = new();
	private readonly List<Calificacion> _calificaciones = new();

	public Guid CurriculaID { get; private set; }
	public Guid? DocenteID { get => Docente?.DocenteID; }
	public SituacionRevista? Docente => DocenteEnFunciones();
	public string Descripcion { get; private set; } = string.Empty;
	public int HorasCatedra { get; private set; }
	public IReadOnlyCollection<SituacionRevista> Docentes => _docentes.AsReadOnly();
	public IReadOnlyCollection<Calificacion> Calificaciones => _calificaciones.AsReadOnly();


	#region CONSTRUCTOR
	private Materia()
		: base() {}

	private Materia(Guid materiaID)
		: base(materiaID) {}

	private Materia(Guid materiaID, Guid curriculaID, string descripcion, int horasCatedra)
		: this(materiaID)
	{
		if (string.IsNullOrWhiteSpace(descripcion))
		{
			throw new ArgumentNullException(nameof(descripcion), "Se debe especificar el nombre de la materia.");
		}

		if (horasCatedra < 1)
		{
			throw new ArgumentException("La materia debe tener al menos una hora cátedra.", nameof(horasCatedra));
		}

		CurriculaID = curriculaID;
		Descripcion = descripcion;
		HorasCatedra = horasCatedra;

		AgregarEvento(new MateriaRegistradaEvent(Id));
	}

	public Materia(Guid curriculaID, string descripcion, int horasCatedra)
		: this(Guid.NewGuid(), curriculaID, descripcion, horasCatedra) {}
	#endregion

	private void EditarDescripcion(string descripcion)
	{
		if (string.IsNullOrWhiteSpace(descripcion))
		{
			throw new ArgumentNullException(nameof(descripcion), "Se debe especificar el nombre de la materia.");
		}

		Descripcion = descripcion;
	}

	private void EditarCargaHoraria(int horasCatedra)
	{
		// TODO: Comprobar que la carga horaria sea divisor de 5 o 10
		if (horasCatedra <= 0)
		{
			throw new ArgumentException("La cantidad de horas cátedras debe ser mayor a cero.", nameof(horasCatedra));
		}

		HorasCatedra = horasCatedra;
	}

	public void ModificarMateria(string descripcion, int horasCatedra)
	{
		EditarDescripcion(descripcion);
		EditarCargaHoraria(horasCatedra);
	}

	#region Docentes
	internal bool DocenteRegistrado(Guid unDocente) =>
		_docentes.Where(x => x.DocenteID.Equals(unDocente) && !x.Periodo.HaFinalizado())
				 .Any();

	private SituacionRevista? BuscarSituacionRevista(Guid unaSituacionRevista)
	{
		if (Guid.Empty.Equals(unaSituacionRevista))
		{
			throw new ArgumentNullException(nameof(unaSituacionRevista), "Se deben especificar la situación de revista que desea buscar.");
		}

		return _docentes.Where(x => x.Id.Equals(unaSituacionRevista))
						.FirstOrDefault();
	}

	private SituacionRevista? DocenteEnFunciones() =>
		_docentes.Where(x => x.Periodo.EstaVigente() && x.EnFunciones)
				 .FirstOrDefault();

	private SituacionRevista? BuscarSituacionRevistaActiva(Guid unDocente)
	{
		if (Guid.Empty.Equals(unDocente))
		{
			throw new ArgumentNullException(nameof(unDocente), "Se deben especificar los datos del docente que desea buscar.");
		}

		return _docentes.Where(x => x.DocenteID.Equals(unDocente) && x.Periodo.EstaVigente())
						.FirstOrDefault();
	}

	private bool CargoDisponible(Cargo unCargo) =>
		!_docentes.Any(x => x.Cargo.Equals(unCargo) && x.Periodo.EstaVigente());

	private void AnularCargoDocente(Guid unaSituacionRevista, Guid unDocente)
	{
		var situacionRevista = BuscarSituacionRevista(unaSituacionRevista);
		if (situacionRevista is null)
		{
			throw new CargoNoEncontradoException();
		}

		situacionRevista.RelevarFuncionesDeAula();
	}

	public int CargosOcupados() =>
		_docentes.Where(x => x.Periodo.EstaVigente()).Count();

	public void RegistrarCargoDocente(Guid unDocente, string unCargo, DateTime fechaInicio, DateTime? fechaFin, bool enFunciones)
	{
		var isValid = Enum.TryParse<Cargo>(unCargo, out Cargo cargo);
		if (!isValid)
		{
			throw new CargoInexistenteException(nameof(unCargo));
		}

		if (!CargoDisponible(cargo))
		{
			throw new CargoOcupadoException();
		}

		var docenteEnFunciones = DocenteEnFunciones();
		if (enFunciones && docenteEnFunciones is not null)
		{
			if (docenteEnFunciones.Cargo.Equals(Cargo.Suplente))
			{
				docenteEnFunciones.Finalizar();
			}
			else
			{
				docenteEnFunciones.RelevarFuncionesDeAula();
			}
		}

		var nuevaSituacionRevista = new SituacionRevista(Id, unDocente, cargo, fechaInicio, fechaFin, enFunciones);
		_docentes.Add(nuevaSituacionRevista);
	}

	public void AsignarDocenteDeAula(Guid unaSituacionRevista)
	{
		var situacionRevista = BuscarSituacionRevista(unaSituacionRevista);
		if (situacionRevista is null)
		{
			throw new CargoNoEncontradoException();
		}

		if (situacionRevista.EnFunciones)
		{
			throw new DocenteEnFuncionesException();
		}

		var docenteEnFunciones = DocenteEnFunciones();
		if (docenteEnFunciones is null)
		{
			situacionRevista.EstablecerEnFuncionesDeAula();
		}
		else
		{
			var esSuplente = docenteEnFunciones.Cargo.Equals(Cargo.Suplente);
			if (esSuplente)
			{
				docenteEnFunciones.Finalizar();
			}
			else
			{
				docenteEnFunciones.RelevarFuncionesDeAula();
			}

			situacionRevista.EstablecerEnFuncionesDeAula();
		}
	}

	public void RelevarDocenteDeAula()
	{
		var unDocente = DocenteEnFunciones();
		if (unDocente is not null)
		{
			unDocente.RelevarFuncionesDeAula();
		}
	}

	public void RescindirCargoDocente(Guid unaSituacionRevista)
	{
		var aRescindir = BuscarSituacionRevista(unaSituacionRevista);
		if (aRescindir is null)
		{
			throw new CargoNoEncontradoException();
		}

		aRescindir.Finalizar();
	}

	public void EliminarCargoDocente(Guid unaSituacionRevista)
	{
		var aEliminar = _docentes.Where(x => x.Id.Equals(unaSituacionRevista))
								 .FirstOrDefault();
		if (aEliminar is null)
		{
			throw new CargoNoEncontradoException();
		}

		_docentes.Remove(aEliminar);
	}
	#endregion

	#region Calificaciones
	private Calificacion? ExisteCalificacion(Guid unCursante, DateTime fecha) =>
		_calificaciones.Where(x => x.CursanteID.Equals(unCursante) && x.Fecha.Date.Equals(fecha.Date))
					   .FirstOrDefault();

	public IReadOnlyCollection<Calificacion> CalificacionesDeParcialesSegunCursante(Guid unCursante) =>
		_calificaciones.Where(x => x.CursanteID.Equals(unCursante) && x.Instancia.Equals(Instancia.Parcial))
					   .ToList();

	public void RegistrarCalificacion(Guid unCursante, DateTime fecha, Instancia instancia, bool asistencia, double? nota, string? observacion)
	{
		var unaCalificacion = ExisteCalificacion(unCursante, fecha);
		if (unaCalificacion is not null)
		{
			throw new ArgumentException($"Ya se encuentra registrada una calificación del alumno en la fecha { unaCalificacion.Fecha.Date }.");
		}

		var nuevaCalificacion = asistencia ? Calificacion.Crear(unCursante, fecha, instancia, nota, observacion) : Calificacion.Inasistencia(unCursante, fecha, instancia, observacion);

		_calificaciones.Add(nuevaCalificacion);
	}

	public void QuitarCalificacion(Guid unCursante, DateTime fecha, Instancia instancia)
	{
		var unaCalificacion = ExisteCalificacion(unCursante, fecha);
		if (unaCalificacion is null)
		{
			throw new ArgumentException($"No se encuentra registrada una calificación del alumno para la fecha {fecha.Date}.");
		}

		_calificaciones.Remove(unaCalificacion);
	}
	#endregion

	#region Horarios
	/*
	 * public int HorasCatedraSinAsignar =>
		HorasCatedra - _horarios.Count();

	public bool HorarioOcupado(Horario horarioBuscado) => _horarios.Contains(horarioBuscado);

	public void AgregarHorario(Turno unTurno, Dia unDia, TimeOnly horaInicio, int duracion)
	{
		if (_horarios.Count >= HorasCatedra)
		{
			throw new ArgumentException("Error al agregar un horario. La materia ya tiene sus horas cátedras completas.");
		}

		var unHorario = Horario.Crear(unTurno, unDia, horaInicio, duracion);
		_horarios.Add(unHorario);
	}

	public void CambiarHorario(Horario viejo, Horario nuevo)
	{
		if (_horarios.Count() == 0)
		{
			throw new MateriaSinHorariosAsignadoException();
		}

		*//*
		 * var horario = _horarios.Find(x => x.HoraInicio == horarioViejo.HoraInicio &&
		 *                            x.HoraFin == horarioViejo.HoraFin &&
		 *                            x.DiaSemana == horarioViejo.DiaSemana);
		 *//*

		if (!_horarios.Contains(viejo))
		{
			throw new ArgumentException("El horario que desea modificar no se encuentra asignado a la materia.", nameof(viejo));
		}

		_horarios.Remove(viejo);
		_horarios.Add(nuevo);
	}

	public bool ExistenHorasCatedraSinAsignar() =>
		HorasCatedraSinAsignar >= 0;
	*/
	#endregion
}
