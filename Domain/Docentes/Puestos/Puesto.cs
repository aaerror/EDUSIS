using Domain.Shared;

namespace Domain.Docentes.Puestos;

public class Puesto : Entity
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

	public Puesto(Guid docenteID, string estado, string posicion, DateTime desde, DateTime? hasta)
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

	public void ActualizarPuestoDocente(string posicion, DateTime desde, DateTime? hasta)
	{
		EstablecerPosicion(posicion);
		EstablecerPeriodo(desde, hasta);
	}

	public void EstablecerComoPuestoFijo()
	{
		if (!EstaActivo())
		{
			throw new ArgumentException($"El puesto docente no se encuentra activo.\nFecha de finalización: { Periodo.FechaFin.Value.ToString("D") }");
		}

		if (!EsEventual)
		{
			throw new ArgumentException("El puesto docente es por tiempo indeterminado.");
		}

		EstablecerPeriodo(Periodo.FechaInicio, null);
	}

	public void Rescindir()
	{
		if (EstaActivo())
		{
			Estado = EstadoPuesto.Inactivo;
			Periodo = Periodo.ActualizarFechaFin(DateTime.Today.Date);
		}
	}

	public void Rescindir(DateTime fechaFin)
	{
		Estado = EstadoPuesto.Inactivo;
		Periodo = Periodo.ActualizarFechaFin(fechaFin);
	}

	public bool EstaActivo() =>
		Periodo.EstaVigente();
}