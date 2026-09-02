using Domain.Curriculas.Exceptions;
using Domain.Shared;

namespace Domain.Curriculas.Materias.CargosDocentes;

public class SituacionRevista : Entity
{
	public Guid MateriaID { get; private set; } = Guid.Empty;
	public Guid DocenteID { get; private set; } = Guid.Empty;
	public EstadoSituacionRevista Estado { get; private set; } = EstadoSituacionRevista.EMPTY;
	public Cargo Cargo { get; private set; }
	public RangoFechas Periodo { get; private set; }
	public bool EnFunciones { get; private set; } = false;


	#region CONSTRUCTOR
	private SituacionRevista() { }

	private SituacionRevista(Guid situacionRevistaID)
		: base(situacionRevistaID) {}

	private SituacionRevista(Guid situacionRevistaID, Guid unaMateria, Guid unDocente, Cargo unCargo, RangoFechas unPeriodo, bool enFunciones)
		: this(situacionRevistaID)
	{
		if (enFunciones && (!unPeriodo.EsIndeterminado() || unPeriodo.HaFinalizado()))
		{
			throw new CargoNoVigenteException();
		}

		if (unPeriodo.EsIndeterminado() && (unCargo.Equals(Cargo.Interino) || unCargo.Equals(Cargo.Suplente)))
		{
			throw new CargoTemporalSinFechaFinalizacionException();
		}

		MateriaID = unaMateria;
		DocenteID = unDocente;
		Cargo = unCargo;
		Estado = EstadoSituacionRevista.Aceptado;
		Periodo = unPeriodo;
		EnFunciones = enFunciones;
	}

	public SituacionRevista(Guid unaMateria, Guid unDocente, Cargo unCargo, DateTime fechaInicio, DateTime? fechaFin, bool enFunciones)
		: this(Guid.NewGuid(), unaMateria, unDocente, unCargo, fechaFin.HasValue ? RangoFechas.Create(fechaInicio, fechaFin.Value) : RangoFechas.Create(fechaInicio), enFunciones)
	{
		/*var isValid = Enum.IsDefined(typeof(Cargo), unCargo);
		if (!isValid)
		{
			throw new CargoInexistenteException(nameof(unCargo));
		}*/

		
	}
	#endregion

	public bool EsCargoVigente() =>
		Periodo.EstaVigente();

	public bool EsTemporal() =>
		!Periodo.EsIndeterminado();

	public void EstablecerEnFuncionesDeAula()
	{
		if (!Periodo.EstaVigente())
		{
			throw new CargoNoVigenteException();
		}

		EnFunciones = true;
	}

	public void RelevarFuncionesDeAula()
	{
		if (!Periodo.EstaVigente())
		{
			throw new CargoNoVigenteException();
		}

		EnFunciones = false;
	}

	public void EstablecerFechaFinalizacion(DateTime fechaFin)
	{
		if (EsTemporal())
		{
			throw new CargoConFechaFinalizacionException();
		}

		Periodo = Periodo.ActualizarFechaFin(fechaFin);
	}

	public void Finalizar()
	{
		if (!Periodo.HaIniciado())
		{
			throw new CargoNoIniciadoException();
		}

		if (Periodo.HaFinalizado())
		{
			throw new CargoNoVigenteException();
		}

		Periodo = Periodo.ActualizarFechaFin(DateTime.Today);
		Estado = EstadoSituacionRevista.Finalizado;
		EnFunciones = false;
	}
}