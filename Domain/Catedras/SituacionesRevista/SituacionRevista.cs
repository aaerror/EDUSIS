using Domain.Catedras.Exceptions;
using Domain.Shared;

namespace Domain.Catedras.SituacionesRevista;

public sealed class SituacionRevista : Entity
{
	public Guid DocenteID { get; private set; } = Guid.Empty;
	public EstadoSituacionRevista Estado { get; private set; } = EstadoSituacionRevista.EMPTY;
	public Cargo Cargo { get; private set; }
	public RangoFechas Periodo { get; private set; }
	public Guid? ReemplazaA { get; private set; }


	#region CONSTRUCTOR
	private SituacionRevista() { }

	private SituacionRevista(Guid situacionRevistaID)
		: base(situacionRevistaID) {}

	private SituacionRevista(
		Guid situacionRevistaID,
		Guid unDocente,
		Cargo unCargo,
		RangoFechas unPeriodo,
		Guid? reemplazaA)
			: this(situacionRevistaID)
	{
		if (Guid.Empty.Equals(unDocente))
		{
			var msg = "Se deben especificar los datos del docente.";
			throw new ArgumentNullException(nameof(unDocente), msg);
		}

		if (unPeriodo.EsIndeterminado() && (unCargo.Equals(Cargo.Interino) || unCargo.Equals(Cargo.Suplente)))
		{
			throw new CargoTemporalSinFechaFinalizacionException();
		}

		DocenteID = unDocente;
		Cargo = unCargo;
		Estado = EstadoSituacionRevista.Aceptado;
		Periodo = unPeriodo;
		ReemplazaA = reemplazaA;
	}

	internal SituacionRevista(
		Guid unDocente,
		Cargo unCargo,
		DateTime fechaInicio,
		DateTime? fechaFin,
		Guid? reemplazaA = null)
			: this(Guid.NewGuid(), unDocente, unCargo, fechaFin.HasValue ? RangoFechas.Create(fechaInicio, fechaFin.Value) : RangoFechas.Create(fechaInicio), reemplazaA) {}

	internal static SituacionRevista Crear(
		Guid unDocente,
		string unCargo,
		DateTime fechaInicio,
		DateTime? fechaFin,
		Guid? reemplazaA = null)
	{
		if (!Enum.TryParse<Cargo>(unCargo, out Cargo cargo))
			throw new CargoInexistenteException(nameof(unCargo));

		return new SituacionRevista(unDocente, cargo, fechaInicio, fechaFin, reemplazaA);
	}
	#endregion

	public bool EsCargoVigente() =>
		Periodo.EstaVigente();

	public bool EsTemporal() =>
		!Periodo.EsIndeterminado();

	internal void EstablecerFechaFinalizacion(DateTime fechaFin)
	{
		if (EsTemporal())
		{
			throw new CargoConFechaFinalizacionException();
		}

		Periodo = Periodo.ActualizarFechaFin(fechaFin);
	}

	internal void Finalizar()
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
	}
}