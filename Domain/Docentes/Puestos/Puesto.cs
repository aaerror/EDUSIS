using Domain.Docentes.Exceptions;
using Domain.Shared;

namespace Domain.Docentes.Puestos;

public sealed class Puesto : Entity
{
	public Guid DocenteID { get; private set; }
	public Posicion Posicion { get; private set; }
	public EstadoPuesto Estado { get; private set; }
	public bool EsEventual { get; private set; }
	public RangoFechas Periodo { get; private set; }


	private Puesto() { }

	private Puesto(Guid puestoID)
		: base(puestoID) { }

	private Puesto(Guid puestoID, Guid docenteID, string estado, string posicion, DateTime desde, DateTime? hasta)
		: this(puestoID)
	{
		if (string.IsNullOrWhiteSpace(posicion))
		{
			throw new ArgumentException("Debe ingresar la posición del cargo docente.");
		}

		var result = Enum.TryParse<EstadoPuesto>(estado, out var estadoPuesto);
		if (!result)
		{
			throw new ArgumentException("Se ingresó un estado de puesto docente incorrecto.", nameof(estado));
		}


		DocenteID = docenteID;
		Estado = estadoPuesto;

		EstablecerPosicion(posicion);
		EstablecerPeriodo(desde, hasta);
	}

	internal Puesto(Guid docenteID, string estado, string posicion, DateTime desde, DateTime? hasta)
		: this(Guid.NewGuid(), docenteID, estado, posicion, desde, hasta) { }

	private void EstablecerPosicion(string nuevaPosicion)
	{
		var result = Enum.TryParse<Posicion>(nuevaPosicion, out Posicion posicion);
		if (!result)
		{
			throw new ArgumentException("Se ingresó una posición incorrecta en el puesto docente.", nameof(nuevaPosicion));
		}

		Posicion = posicion;
	}

	private void EstablecerPeriodo(DateTime desde, DateTime? hasta)
	{
		if (DateTime.Today.Date > desde.Date)
		{
			throw new ArgumentException("La fecha de alta del cargo docente no puede ser anterior al día de la fecha.");
		}

		Periodo = !hasta.HasValue ? RangoFechas.Create(desde) : RangoFechas.Create(desde, hasta.Value);
		EsEventual = hasta.HasValue;
	}

	internal void ActualizarPuestoDocente(string posicion, DateTime desde, DateTime? hasta)
	{
		EstablecerPosicion(posicion);
		EstablecerPeriodo(desde, hasta);
	}

	/// <summary>
	/// Confirma el puesto: lo pasa de eventual a por tiempo indeterminado.
	/// </summary>
	/// <remarks>
	/// Las guardas lanzan las excepciones propias del módulo y no <c>ArgumentException</c> del BCL:
	/// la regla "no se confirma un puesto que ya está confirmado" la nombra
	/// <c>DocenteConfirmadoEnCargoDocenteException</c>.
	/// </remarks>
	internal void EstablecerComoPuestoFijo()
	{
		if (!EstaActivo())
		{
			// El motivo se discrimina en lugar de leer FechaFin a ciegas, y no es cosmético: hasta
			// interpolar `Periodo.FechaFin.Value` daría por hecho que un
			// puesto no activo es un puesto finalizado. Es al revés. `EstablecerPeriodo` rechaza
			// toda fecha de inicio anterior a hoy y `RangoFechas` exige fin >= inicio, así que
			// FechaFin nunca queda antes de hoy y `HaFinalizado()` no puede ser cierto para un
			// puesto recién construido: el único camino a `!EstaActivo()` es un puesto que todavía
			// no inició. Y si ese puesto es por tiempo indeterminado, FechaFin es nulo y la guarda
			// moría con InvalidOperationException en vez de con su propia excepción.
			var motivo = Periodo.HaFinalizado()
				? $"finalizó el { Periodo.FechaFin!.Value.ToString("D") }"
				: $"todavía no inició, comienza el { Periodo.FechaInicio.ToString("D") }";

			throw new PuestoDocenteInactivoException(motivo);
		}

		if (!EsEventual)
		{
			throw new DocenteConfirmadoEnCargoDocenteException(nameof(EsEventual));
		}

		EstablecerPeriodo(Periodo.FechaInicio, null);
	}

	internal void Rescindir()
	{
		if (EstaActivo())
		{
			Estado = EstadoPuesto.Inactivo;
			Periodo = Periodo.ActualizarFechaFin(DateTime.Today.Date);
		}
	}

	internal void Rescindir(DateTime fechaFin)
	{
		Estado = EstadoPuesto.Inactivo;
		Periodo = Periodo.ActualizarFechaFin(fechaFin);
	}

	public bool EstaActivo() =>
		Periodo.EstaVigente();
}