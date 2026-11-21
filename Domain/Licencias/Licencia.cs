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

	private Licencia(
		Guid licenciaID,
		Guid docenteID,
		Articulo articulo,
		Estado estado,
		RangoFechas intervalo,
		string? observacion)
			: this(licenciaID)
	{
		ValidarPlazo(articulo, intervalo);

		DocenteID = docenteID;
		Articulo = articulo;
		Estado = estado;
		Periodo = intervalo;
		ModificarObservaciones(observacion);

		AgregarEvento(new LicenciaSolicitadaEvent(Id, DocenteID));
	}

	public Licencia(Guid docenteID, string articulo, DateTime fechaInicio, string? observacion)
		: this(
			Guid.NewGuid(),
			docenteID,
			ConvertirArticulo(articulo),
			Estado.Pendiente,
			RangoFechas.Create(fechaInicio),
			observacion)
	{
		if (fechaInicio.Date > Reloj.Hoy)
		{
			throw new FechaDeInicioDeLicenciaFuturaException();
		}
	}
	
	public Licencia(
		Guid docenteID,
		string articulo,
		DateTime fechaInicio,
		DateTime fechaFin,
		string? observacion)
			: this(
				Guid.NewGuid(),
				docenteID,
				ConvertirArticulo(articulo),
				Estado.Pendiente,
				RangoFechas.Create(fechaInicio,
				fechaFin),
				observacion) { }

	/// <summary>
	/// Solicitud expresada como la carga el usuario: fecha de inicio y días corridos. Cero días es
	/// una licencia sin fecha de fin, que sólo admiten los artículos de plazo abierto.
	/// </summary>
	public Licencia(
		Guid docenteID,
		string articulo,
		DateTime fechaInicio,
		int cantidadDias,
		string? observacion)
			: this(
				Guid.NewGuid(),
				docenteID,
				ConvertirArticulo(articulo),
				Estado.Pendiente,
				PeriodoSegun(fechaInicio, cantidadDias),
				observacion)
	{
		if (fechaInicio.Date > Reloj.Hoy)
		{
			throw new FechaDeInicioDeLicenciaFuturaException();
		}
	}

	private static Articulo ConvertirArticulo(string articulo) =>
		Enumerados.Convertir<Articulo>(articulo, valor => new ArticuloInexistenteException(valor));

	// Días corridos (Ley 4356, art. 25): el fin es el inicio más los días.
	private static RangoFechas PeriodoSegun(DateTime fechaInicio, int cantidadDias) =>
		cantidadDias > 0
			? RangoFechas.Create(fechaInicio, fechaInicio.AddDays(cantidadDias))
			: RangoFechas.Create(fechaInicio);
	#endregion

	public PlazoLegal PlazoLegal =>
		PlazoLegal.De(Articulo);

	/// <summary>
	/// El período tiene que respetar el plazo que la norma asigna al artículo. Se invoca en cada
	/// operación que cambia el artículo o el período, ANTES de asignarlos: una licencia inválida no
	/// llega a existir ni a quedar a medio modificar.
	/// </summary>
	/// <exception cref="LicenciaIndefinidaNoAdmitidaException">Sin fecha de fin en un artículo con plazo.</exception>
	/// <exception cref="PlazoFijoDeLicenciaIncumplidoException">Días distintos de los que fija la norma.</exception>
	/// <exception cref="PlazoMaximoDeLicenciaExcedidoException">Más días que el tope de la norma.</exception>
	private static void ValidarPlazo(Articulo articulo, RangoFechas periodo)
	{
		var plazo = PlazoLegal.De(articulo);

		if (plazo.Tipo == TipoDePlazo.Abierto)
		{
			return;
		}

		if (periodo.EsIndeterminado())
		{
			throw new LicenciaIndefinidaNoAdmitidaException(plazo);
		}

		var dias = (periodo.FechaFin!.Value.Date - periodo.FechaInicio.Date).Days;

		if (plazo.Tipo == TipoDePlazo.Fijo && dias != plazo.Dias)
		{
			throw new PlazoFijoDeLicenciaIncumplidoException(plazo);
		}

		if (plazo.Tipo == TipoDePlazo.Maximo && dias > plazo.Dias)
		{
			throw new PlazoMaximoDeLicenciaExcedidoException(plazo);
		}
	}

	public bool EstaActiva() =>
		Estado.Equals(Estado.Activa) && Periodo.EstaVigente();

	public bool EsIndefinida() =>
		Periodo.EsIndeterminado();

	#region Estado
	public void AprobarLicencia(string? observacion)
	{
		if (!Estado.Equals(Estado.Pendiente))
		{
			throw new LicenciaNoPendienteException();
		}

		Estado = Estado.Activa;
		ModificarObservaciones(observacion);

		AgregarEvento(new LicenciaActivadaEvent(Id, DocenteID));
	}

	public void CancelarLicencia(string? observacion)
	{
		if (!Estado.Equals(Estado.Pendiente))
		{
			throw new LicenciaNoPendienteException();
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
		Periodo = Periodo.ActualizarFechaFin(Reloj.Hoy);

		AgregarEvento(new LicenciaFinalizadaEvent(Id, DocenteID));
	}

	/// <summary>
	/// Una licencia se borra sólo mientras está pendiente de aprobación: una vez aprobada, cancelada o
	/// finalizada forma parte del legajo del docente y su baja sería perder un antecedente.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Molde de <c>PlanillaAsistencia.ValidarSePuedeEliminar</c> y <c>Curso.ValidarSePuedeEliminar</c>:
	/// <strong>valida y no cambia estado</strong>, porque la baja de una licencia es borrado físico y no
	/// una transición. El servicio la invoca y después borra; no lee <c>Estado</c> ni decide nada.
	/// </para>
	/// <para>
	/// La regla vive acá y no en el servicio: las capas superiores no repiten ni suplen las
	/// validaciones del dominio, así que <c>ServicioLicencia</c> sólo invoca este método y después borra.
	/// </para>
	/// </remarks>
	/// <exception cref="LicenciaNoPendienteException">La licencia ya no está pendiente.</exception>
	public void ValidarSePuedeEliminar()
	{
		if (!Estado.Equals(Estado.Pendiente))
		{
			throw new LicenciaNoPendienteException();
		}
	}
	#endregion

	public double DuracionEn(Tiempo tiempo) =>
		Periodo.Duracion(tiempo);

	public void EstablecerFechaFinalizacion(DateTime fechaFin)
	{
		if (fechaFin.Date <= Reloj.Hoy)
		{
			throw new FechaDeFinalizacionNoPosteriorAHoyException();
		}

		if (!EstaActiva())
		{
			throw new LicenciaInactivaException();
		}

		if (!EsIndefinida())
		{
			throw new LicenciaConFechaDeFinalizacionException();
		}

		var nuevoPeriodo = Periodo.ActualizarFechaFin(fechaFin);
		ValidarPlazo(Articulo, nuevoPeriodo);

		Periodo = nuevoPeriodo;
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

		var nuevoPeriodo = RangoFechas.Create(Periodo.FechaInicio);
		ValidarPlazo(Articulo, nuevoPeriodo);

		Periodo = nuevoPeriodo;
	}

	/// <summary>
	/// Corrige la solicitud mientras no está activa: artículo, inicio y días se cambian juntos porque
	/// el plazo válido depende del artículo.
	/// </summary>
	/// <remarks>
	/// Reemplaza a <c>ModificarPeriodo</c>, que no recibía el artículo: la pantalla de edición lo
	/// dejaba elegir y el cambio se descartaba en silencio.
	/// </remarks>
	public void ModificarSolicitud(string articulo, DateTime fechaInicio, int cantidadDias)
	{
		if (EstaActiva())
		{
			throw new LicenciaActivaException();
		}

		var nuevoArticulo = ConvertirArticulo(articulo);
		var nuevoPeriodo = PeriodoSegun(fechaInicio, cantidadDias);
		ValidarPlazo(nuevoArticulo, nuevoPeriodo);

		Articulo = nuevoArticulo;
		Periodo = nuevoPeriodo;
	}

	public void ExtenderLicencia(int cantidadDias)
	{
		if (cantidadDias <= 0)
		{
			throw new ExtensionSinDiasException();
		}

		if (!EstaActiva())
		{
			throw new LicenciaInactivaException();
		}

		if (EsIndefinida())
		{
			throw new LicenciaIndefinidaException();
		}

		var nuevoPeriodo = Periodo.ActualizarFechaFin(Periodo.FechaFin!.Value.AddDays(cantidadDias));
		ValidarPlazo(Articulo, nuevoPeriodo);

		Periodo = nuevoPeriodo;
	}

	public void ModificarObservaciones(string? observacion)
	{
		if (!string.IsNullOrWhiteSpace(observacion))
		{
			if (observacion.Trim().Length > 250)
			{
				var msg = "La observación no debe superar los 250 caracteres.";
				throw new ExcesoCaracteresException(msg);
			}
		}

		Observacion = observacion;
	}
}