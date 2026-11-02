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

		// EL INVARIANTE VA EN LAS DOS DIRECCIONES. "El cargo a término es interino o suplente" es una
		// equivalencia, y acá estaba escrita una sola mitad: la que prohíbe el cargo temporal sin
		// término. Sin la otra, el alta dejaba crear un titular CON fecha de fin —un titular a
		// término, que es lo que el negocio llama interino—, de modo que el mismo estado que
		// PasarAInterinoHasta declara ilegal se alcanzaba por la puerta de al lado.
		if (unPeriodo.EsIndeterminado() && (unCargo.Equals(Cargo.Interino) || unCargo.Equals(Cargo.Suplente)))
		{
			throw new CargoTemporalSinFechaFinalizacionException();
		}

		if (!unPeriodo.EsIndeterminado() && unCargo.Equals(Cargo.Titular))
		{
			throw new TitularConFechaFinalizacionException();
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

	/// <summary>
	/// Titular e interino son dos formas de ocupar el mismo puesto; el suplente no lo ocupa, reemplaza
	/// a quien corresponda mientras dura la ausencia.
	/// </summary>
	/// <remarks>
	/// La regla vivía como un método privado de <see cref="Catedra"/>, que es donde se la necesitaba
	/// para decidir un alta. Sube acá porque es una propiedad DEL CARGO y no de la cátedra, y porque
	/// la pantalla necesita la misma distinción para saber qué designaciones pueden ir al frente del
	/// curso sin reimplementarla contra el texto del enum.
	/// </remarks>
	public static bool EsCargoOcupante(Cargo unCargo) =>
		unCargo.Equals(Cargo.Titular) || unCargo.Equals(Cargo.Interino);

	public bool EsOcupante() =>
		EsCargoOcupante(Cargo);

	/// <summary>
	/// Le pone término a una designación indeterminada: el cargo pasa a ser interino y vence en
	/// <paramref name="fechaFin"/>.
	/// </summary>
	/// <remarks>
	/// SE LLAMABA <c>EstablecerFechaFinalizacion</c> Y SÓLO MOVÍA EL PERÍODO. El cargo quedaba en
	/// <see cref="Cargo.Titular"/> con una fecha de fin encima, que es un estado que el negocio no
	/// reconoce: un cargo a término es interino. Es el mismo estado que el constructor rechaza en el
	/// alta con <see cref="TitularConFechaFinalizacionException"/>; mientras esta operación no mudara
	/// el cargo, lo que el alta prohibía se alcanzaba igual por acá.
	///
	/// Por eso el único cargo sobre el que puede correr es el titular: el interino y el suplente
	/// nacen con fecha de fin, <see cref="EsTemporal"/> ya les da <c>true</c> y se van por
	/// <see cref="CargoConFechaFinalizacionException"/>.
	///
	/// El término tiene que ser posterior a hoy. Un fin que cae hoy no agenda nada: cierra la
	/// designación en el acto, que es lo que hace <see cref="Finalizar"/>. Mismo criterio —y mismo
	/// mensaje— que <c>Licencia.EstablecerFechaFinalizacion</c>.
	/// </remarks>
	internal void PasarAInterinoHasta(DateTime fechaFin)
	{
		if (EsTemporal())
		{
			throw new CargoConFechaFinalizacionException();
		}

		if (fechaFin.Date <= DateTime.Today.Date)
		{
			var msg = "La fecha de finalización debe ser posterior al día de hoy.";
			throw new ArgumentException(msg);
		}

		Cargo = Cargo.Interino;
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