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
	/// El suplente no ocupa el puesto, reemplaza a quien corresponda mientras dura la ausencia.
	/// </summary>
	private static bool EsOcupante(Cargo unCargo) =>
		unCargo.Equals(Cargo.Titular) || unCargo.Equals(Cargo.Interino);

	private SituacionRevista? OcupanteVigente() =>
		_situaciones.FirstOrDefault(situacion => EsOcupante(situacion.Cargo) && situacion.EsCargoVigente());

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

			if (_situaciones.Any(situacion => reemplazaA.Value.Equals(situacion.ReemplazaA) && situacion.EsCargoVigente()))
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

	public void EstablecerFinDeDesignacion(Guid unaSituacionRevista, DateTime fechaFin)
	{
		var situacionRevista = _situaciones.FirstOrDefault(situacion => situacion.Id.Equals(unaSituacionRevista));
		if (situacionRevista is null)
		{
			throw new SituacionRevistaNoEncontradaException();
		}

		situacionRevista.EstablecerFechaFinalizacion(fechaFin);
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
