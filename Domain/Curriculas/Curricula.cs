using Domain.Curriculas.Exceptions;
using Domain.Curriculas.Materias.CargosDocentes;
using Domain.Curriculas.Materias;
using Domain.Shared;

namespace Domain.Curriculas;

public sealed class Curricula : Entity
{
	private readonly List<Materia> _materias = new();

	public Guid CursoID { get; private set; } = Guid.Empty;
	public RangoFechas Periodo { get; private set; }
	public int TotalHorasSemanales => _materias.Select(x => x.HorasCatedra).Sum();
	public int TotalEspacios => _materias.Count;

	public IReadOnlyCollection<Materia> Materias => _materias.AsReadOnly();


	#region CONSTRUCTOR
	private Curricula() {}

	private Curricula(Guid curriculaID)
		: base(curriculaID) {}

	private Curricula(Guid curriculaID, Guid unCurso, DateTime fechaInicio, DateTime? fechaFin)
		: this(curriculaID)
	{
		CursoID = unCurso;
		Periodo = fechaFin.HasValue ? RangoFechas.Create(fechaInicio, fechaFin.Value) : RangoFechas.Create(fechaInicio);
	}

	public Curricula(Guid unCurso, DateTime fechaInicio, DateTime? fechaFin)
		: this(Guid.NewGuid(), unCurso, fechaInicio, fechaFin) { }
	#endregion

	public bool EstaVigente() =>
		Periodo.EstaVigente();

	public void Desafectar() =>
		EstablecerFechaFinalizacion(DateTime.Today);

	public void EstablecerFechaFinalizacion(DateTime fechaFin)
	{
		if (!Periodo.EstaVigente())
		{
			throw new CurriculaNoVigenteException();
		}

		Periodo = Periodo.ActualizarFechaFin(fechaFin);
	}

	#region Materias
	private bool ExisteNombreMateria(string descripcion) =>
		_materias.Any(x => string.Equals(x.Descripcion, descripcion, StringComparison.OrdinalIgnoreCase));

	private bool ExisteMateria(Guid unaMateria)
	{
		if (Guid.Empty.Equals(unaMateria))
		{
			throw new MateriaDuplicadaException(unaMateria.ToString());
		}

		return _materias.Any(x => x.Id.Equals(unaMateria));
	}

	private Materia? BuscarMateria(Guid unaMateria)
	{
		if (!ExisteMateria(unaMateria))
		{
			throw new MateriaNoEncontradaException(unaMateria.ToString());
		}

		return _materias.Find(x => x.Id.Equals(unaMateria));
	}

	public void AgregarMateria(string descripcion, int horasCatedra)
	{
		if (!Periodo.EstaVigente())
		{
			throw new CurriculaNoVigenteException();
		}

		var esDuplicado = ExisteNombreMateria(descripcion);
		if (esDuplicado)
		{
			throw new NombreMateriaDuplicadoException(descripcion);
		}

		_materias.Add(new(Id, descripcion, horasCatedra));
	}

	public void ActualizarMateria(Guid unaMateria, string descripcion, int horasCatedra)
	{
		if (!Periodo.EstaVigente())
		{
			throw new CurriculaNoVigenteException();
		}

		var esDuplicado = _materias.Any(x => !x.Id.Equals(unaMateria) && string.Equals(x.Descripcion.ToLower(), descripcion.ToLower()));
		if (esDuplicado)
		{
			throw new NombreMateriaDuplicadoException(descripcion);
		}

		var materia = BuscarMateria(unaMateria);
		materia.ModificarMateria(descripcion, horasCatedra);
	}

	public void QuitarMateria(Guid aEliminar)
	{
		if (!Periodo.EstaVigente())
		{
			throw new CurriculaNoVigenteException();
		}

		var materia = BuscarMateria(aEliminar);
		_materias.Remove(materia);
	}

	public int HorasSemanales() =>
		_materias.Select(x => x.HorasCatedra)
				 .Sum();

	public void AgregarCalificacionDeCursante(Guid unaMateria, Guid unCursante, DateTime fecha, Instancia instancia, bool asistencia, double? nota, string? observacion)
	{
		var materia = BuscarMateria(unaMateria);
		materia.RegistrarCalificacion(unCursante, fecha, instancia, asistencia, nota, observacion);
	}

	public void QuitarCalificacionDeCursante(Guid unaMateria, Guid unCursante, DateTime fecha, Instancia instancia)
	{
		var materia = BuscarMateria(unaMateria);
		materia.QuitarCalificacion(unCursante, fecha, instancia);
	}
	#endregion

	#region Docentes
	public IReadOnlyCollection<SituacionRevista> DocentesAsignadosEnMateria(Guid unaMateria)
	{
		var materias = BuscarMateria(unaMateria);

		return materias.Docentes;
	}

	public void AsignarDocenteEnMateria(Guid unaMateria, Guid unDocente, string unCargo, DateTime fechaAlta, DateTime? fechaBaja, bool enFunciones)
	{
		var materia = BuscarMateria(unaMateria);

		var estaRegistrado = materia.DocenteRegistrado(unDocente);
		if (estaRegistrado)
		{
			throw new DocenteRegistradoException();
		}

		materia.RegistrarCargoDocente(unDocente, unCargo, fechaAlta, fechaBaja, enFunciones);
	}

	public void RelevarDocenteDeMateria(Guid unaMateria)
	{
		var materia = BuscarMateria(unaMateria);
		materia.RelevarDocenteDeAula();
	}

	public void RescindirDocenteDeMateria(Guid unaMateria, Guid unaSituacionRevista)
	{
		var materia = BuscarMateria(unaMateria);
		materia.RescindirCargoDocente(unaSituacionRevista);
	}

	public void EliminarCargoDocenteDeMateria(Guid unaMateria, Guid unaSituacionRevista)
	{
		var materia = BuscarMateria(unaMateria);
		materia.EliminarCargoDocente(unaSituacionRevista);
	}

	public void EstablecerDocenteEnFuncionesEnMateria(Guid unaMateria, Guid unaSituacionRevista)
	{
		var materia = BuscarMateria(unaMateria);
		materia.AsignarDocenteDeAula(unaSituacionRevista);
	}
	#endregion
}
