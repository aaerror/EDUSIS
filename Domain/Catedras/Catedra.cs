using Domain.Catedras.DomainEvents;
using Domain.Catedras.Exceptions;
using Domain.Catedras.Horarios.Exceptions;
using Domain.Catedras.Horarios;
using Domain.Catedras.SituacionesRevista;
using Domain.Shared;

namespace Domain.Catedras;

public sealed class Catedra : Entity
{
	private readonly List<Horario> _horarios = new();
	private readonly List<SituacionRevista> _situaciones = new();

	public Guid MateriaID { get; private set; } = Guid.Empty;
	public Guid DivisionID { get; private set; } = Guid.Empty;
	public int CargaHoraria { get; private set; }
	public Guid? SituacionEnFuncionesID { get; private set; }
	public IReadOnlyCollection<Horario> Horarios => _horarios.AsReadOnly();
	public IReadOnlyCollection<SituacionRevista> SituacionesRevista => _situaciones.AsReadOnly();


	#region CONSTRUCTOR
	private Catedra() { }

	private Catedra(Guid catedraID)
		: base(catedraID) {}

	private Catedra(Guid catedraID, Guid unaMateria, Guid unaDivision, int cargaHoraria)
		: this(catedraID)
	{
		if (Guid.Empty.Equals(unaMateria))
		{
			throw new SinDatosMateriaException();
		}

		if (Guid.Empty.Equals(unaDivision))
		{
			throw new SinDatosDivisionException();
		}

		if (cargaHoraria < 1)
		{
			throw new CargaHorariaInvalidaException();
		}

		MateriaID = unaMateria;
		DivisionID = unaDivision;
		CargaHoraria = cargaHoraria;
	}

	public Catedra(Guid unaMateria, Guid unaDivision, int cargaHoraria)
		: this(Guid.NewGuid(), unaMateria, unaDivision, cargaHoraria) {}
	#endregion

	#region Designaciones
	/// <summary>
	/// Titular e interino son dos formas de ocupar el mismo puesto: hay a lo sumo uno vigente.
	/// La regla de qué cargo ocupa el puesto vive en <see cref="SituacionRevista.EsCargoOcupante"/>.
	/// </summary>
	private static bool EsOcupante(Cargo unCargo) =>
		SituacionRevista.EsCargoOcupante(unCargo);

	private SituacionRevista? OcupanteVigente() =>
		_situaciones.FirstOrDefault(situacion => EsOcupante(situacion.Cargo) && situacion.EsCargoVigente());

	/// <summary>
	/// La designación indicada ya tiene un suplente vigente cubriéndola.
	/// </summary>
	/// <remarks>
	/// Es la condición que <see cref="Designar"/> usa para rechazar una segunda suplencia sobre el
	/// mismo ausente con <c>DocenteYaReemplazadoException</c>. Es pública porque la pantalla de
	/// designaciones necesita la MISMA regla para no ofrecer en el combo a quien ya está reemplazado:
	/// ofrecer una opción que el dominio va a rechazar es hacerle descubrir la regla a fuerza de
	/// cartel de error. Reusarla es lo que garantiza que la UI y el dominio no se separen.
	/// </remarks>
	public bool TieneSuplenteVigente(Guid unaSituacionRevista) =>
		_situaciones.Any(situacion => unaSituacionRevista.Equals(situacion.ReemplazaA) && situacion.EsCargoVigente());

	/// <summary>El docente de la designación indicada, o <c>null</c> si no pertenece a la cátedra.</summary>
	public Guid? DocenteDe(Guid unaSituacionRevista) =>
		_situaciones.FirstOrDefault(situacion => situacion.Id.Equals(unaSituacionRevista))?.DocenteID;

	public Guid Designar(Guid unDocente, Cargo unCargo, DateTime fechaInicio, DateTime? fechaFin, Guid? reemplazaA = null)
	{
		if (_situaciones.Any(situacion => situacion.DocenteID.Equals(unDocente) && situacion.EsCargoVigente()))
		{
			throw new DocenteRegistradoException();
		}

		if (EsOcupante(unCargo))
		{
			if (reemplazaA.HasValue)
			{
				throw new OcupanteConReemplazoException();
			}

			if (OcupanteVigente() is not null)
			{
				throw new CargoOcupadoException();
			}
		}
		else
		{
			if (!reemplazaA.HasValue)
			{
				throw new SuplenciaSinReemplazoException();
			}

			var reemplazado = _situaciones.FirstOrDefault(situacion => situacion.Id.Equals(reemplazaA.Value));
			if (reemplazado is null || !reemplazado.EsCargoVigente())
			{
				throw new SituacionRevistaNoEncontradaException();
			}

			if (TieneSuplenteVigente(reemplazaA.Value))
			{
				throw new DocenteYaReemplazadoException();
			}
		}

		var situacionRevista = new SituacionRevista(unDocente, unCargo, fechaInicio, fechaFin, reemplazaA);
		_situaciones.Add(situacionRevista);

		return situacionRevista.Id;
	}

	public void PonerEnFunciones(Guid unaSituacionRevista)
	{
		var situacionRevista = _situaciones.FirstOrDefault(situacion => situacion.Id.Equals(unaSituacionRevista));
		if (situacionRevista is null)
		{
			throw new SituacionRevistaNoEncontradaException();
		}

		if (!situacionRevista.EsCargoVigente())
		{
			throw new CargoNoVigenteException();
		}

		if (SituacionEnFuncionesID.HasValue && SituacionEnFuncionesID.Value.Equals(unaSituacionRevista))
		{
			throw new DocenteEnFuncionesException();
		}

		SituacionEnFuncionesID = unaSituacionRevista;
	}

	public void RelevarDeFunciones()
	{
		if (!SituacionEnFuncionesID.HasValue)
		{
			throw new DocenteSinCargoException();
		}

		SituacionEnFuncionesID = null;
		AgregarEvento(new CatedraSinDocenteEnFuncionesEvent(Id));
	}

	/// <summary>
	/// Le pone término a una designación indeterminada de la cátedra: su cargo pasa a interino y
	/// vence en <paramref name="fechaFin"/>.
	/// </summary>
	public void PasarAInterinoHasta(Guid unaSituacionRevista, DateTime fechaFin)
	{
		var situacionRevista = _situaciones.FirstOrDefault(situacion => situacion.Id.Equals(unaSituacionRevista));
		if (situacionRevista is null)
		{
			throw new SituacionRevistaNoEncontradaException();
		}

		situacionRevista.PasarAInterinoHasta(fechaFin);
	}

	public void FinalizarDesignacion(Guid unaSituacionRevista)
	{
		var situacionRevista = _situaciones.FirstOrDefault(situacion => situacion.Id.Equals(unaSituacionRevista));
		if (situacionRevista is null)
		{
			throw new SituacionRevistaNoEncontradaException();
		}

		situacionRevista.Finalizar();

		if (SituacionEnFuncionesID.HasValue && SituacionEnFuncionesID.Value.Equals(unaSituacionRevista))
		{
			SituacionEnFuncionesID = null;
			AgregarEvento(new CatedraSinDocenteEnFuncionesEvent(Id));
		}
	}

	public Guid? DocenteEnFunciones() =>
		SituacionEnFuncionesID.HasValue
			? _situaciones.FirstOrDefault(situacion => situacion.Id.Equals(SituacionEnFuncionesID.Value))?.DocenteID
			: null;
	#endregion

	#region Horarios
	public void AgregarHorario(Horario unHorario)
	{
		if (_horarios.Contains(unHorario))
		{
			throw new HorarioDuplicadoException();
		}

		if (HorasAsignadas >= CargaHoraria)
		{
			throw new HorasCatedraCompletasException();
		}

		_horarios.Add(unHorario);
	}

	public void QuitarHorario(Horario unHorario)
	{
		if (!_horarios.Contains(unHorario))
		{
			throw new HorarioNoAsignadoException();
		}

		_horarios.Remove(unHorario);
	}

	public int HorasAsignadas =>
		_horarios.Count;

	public int HorasSinAsignar =>
		CargaHoraria - HorasAsignadas;

	public bool TieneHorasCompletas() =>
		HorasAsignadas >= CargaHoraria;
	#endregion
}
