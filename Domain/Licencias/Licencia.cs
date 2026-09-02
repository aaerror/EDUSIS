using Domain.Licencias.DomainEvents;
using Domain.Licencias.Exceptions;
using Domain.Shared.Exceptions;
using Domain.Shared;

namespace Domain.Licencias;

public sealed class Licencia : Entity
{
	public Guid DocenteID { get; private set; }
	public Articulo Articulo { get; private set; }
	public Estado Estado { get; private set; }
	public RangoFechas Periodo { get; private set; }
	public string? Observacion { get; private set; } = string.Empty;


	#region CONSTRUCTOR
	private Licencia()
		: base() { }

	private Licencia(Guid licenciaID)
		: base(licenciaID) { }

	private Licencia(Guid licenciaID, Guid docenteID, Articulo articulo, Estado estado, RangoFechas intervalo, string? observacion)
		: this(licenciaID)
	{
		DocenteID = docenteID;
		Articulo = articulo;
		Estado = estado;
		Periodo = intervalo;
		ModificarObservaciones(observacion);

		AgregarEvento(new LicenciaSolicitadaEvent(Id, DocenteID));
	}

	public Licencia(Guid docenteID, string articulo, DateTime fechaInicio, string? observacion)
		: this(Guid.NewGuid(), docenteID, Enum.Parse<Articulo>(articulo), Estado.Pendiente, RangoFechas.Create(fechaInicio), observacion)
	{
		if (fechaInicio.Date > DateTime.Today.Date)
		{
			throw new ArgumentException($"La fecha de inicio no debe mayor a la fecha de hoy ({DateTime.Today.Date.ToString("D")})");
		}
	}
	
	public Licencia(Guid docenteID, string articulo, DateTime fechaInicio, DateTime fechaFin, string? observacion)
		: this(Guid.NewGuid(), docenteID, Enum.Parse<Articulo>(articulo), Estado.Pendiente, RangoFechas.Create(fechaInicio, fechaFin), observacion) { }
	#endregion

	public bool EstaActiva() =>
		Estado.Equals(Estado.Activa) && Periodo.EstaVigente();

	public bool EsIndefinida() =>
		!Periodo.EsIndeterminado();

	#region Estado
	public void AprobarLicencia(string? observacion)
	{
		if (!Estado.Equals(Estado.Pendiente))
		{
			throw new ArgumentException("La licencia no se encuentra pendiente.");
		}

		Estado = Estado.Activa;
		ModificarObservaciones(observacion);

		AgregarEvento(new LicenciaActivadaEvent(Id, DocenteID));
	}

	public void CancelarLicencia(string? observacion)
	{
		if (!Estado.Equals(Estado.Pendiente))
		{
			throw new ArgumentException("La licencia no se encuentra pendiente.");
		}

		Estado = Estado.Cancelada;
		ModificarObservaciones(observacion);

		AgregarEvento(new LicenciaCanceladaEvent(Id, DocenteID));
	}

	public void FinalizarLicencia()
	{
		if (!EstaActiva())
		{
			throw new LicenciaInactivaException();
		}

		Estado = Estado.Finalizada;
		Periodo = Periodo.ActualizarFechaFin(DateTime.Today);

		AgregarEvento(new LicenciaFinalizadaEvent(Id, DocenteID));
	}
	#endregion

	public double DuracionEn(Tiempo tiempo) =>
		Periodo.Duracion(tiempo);

	public void EstablecerFechaFinalizacion(DateTime fechaFin)
	{
		if (fechaFin.Date <= DateTime.Today.Date)
		{
			throw new ArgumentException("La fecha de finalización debe ser posterior al día de hoy.");
		}

		if (!EstaActiva())
		{
			throw new LicenciaInactivaException();
		}

		if (!EsIndefinida())
		{
			throw new ArgumentException("La licencia ya posee una fecha de finalizacion.");
		}

		Periodo = Periodo.ActualizarFechaFin(fechaFin);
	}

	public void EstablecerLicenciaIndefinida()
	{
		if (!EstaActiva())
		{
			throw new LicenciaInactivaException();
		}

		if (EsIndefinida())
		{
			throw new LicenciaIndefinidaException();
		}

		Periodo = RangoFechas.Create(Periodo.FechaInicio);
	}

	public void ModificarPeriodo(DateTime fechaInicio, int cantidadDias)
	{
		if (EstaActiva())
		{
			throw new LicenciaActivaException();
		}

		Periodo = cantidadDias > 0 ? RangoFechas.Create(fechaInicio, fechaInicio.AddDays(cantidadDias)) : RangoFechas.Create(fechaInicio);
	}

	public void ExtenderLicencia(int cantidadDias)
	{
		if (cantidadDias <= 0)
		{
			throw new ArgumentException("Se debe especificar la cantidad de días que quiere extender la licencia.");
		}

		if (!EstaActiva())
		{
			throw new LicenciaInactivaException();
		}

		if (EsIndefinida())
		{
			throw new LicenciaIndefinidaException();
		}

		var nuevaFechaFin = Periodo.FechaFin.Value.AddDays(cantidadDias);
		Periodo = Periodo.ActualizarFechaFin(nuevaFechaFin);
	}

	public void ModificarObservaciones(string? observacion)
	{
		if (!string.IsNullOrWhiteSpace(observacion))
		{
			if (observacion.Trim().Length > 250)
			{
				throw new ExcesoCaracteresException("La observación no debe superar los 250 caracteres.");
			}
		}

		Observacion = observacion;
	}
}