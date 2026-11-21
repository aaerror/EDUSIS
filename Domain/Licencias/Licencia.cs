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

	// Días corridos (Ley 4356, art. 25) y la fecha de fin es el ÚLTIMO día de licencia: 15 días
	// desde el 01/03 terminan el 15/03. Sumar los días sin restar uno daba un día de más, y con
	// él dos licencias consecutivas compartían un día.
	private static RangoFechas PeriodoSegun(DateTime fechaInicio, int cantidadDias) =>
		cantidadDias > 0
			? RangoFechas.Create(fechaInicio.Date, fechaInicio.Date.AddDays(cantidadDias - 1))
			: RangoFechas.Create(fechaInicio.Date);

	/// <summary>
	/// Días de licencia del período, contando el de inicio y el de fin. Cero si es indefinido.
	/// </summary>
	/// <remarks>
	/// No es <c>RangoFechas.Duracion</c>: esa mide la diferencia entre fechas (el día de fin no se
	/// suma), que es la cuenta de los demás agregados. Una licencia de un día empieza y termina el
	/// mismo día.
	/// </remarks>
	private static int DiasDe(RangoFechas periodo) =>
		periodo.EsIndeterminado()
			? 0
			: (periodo.FechaFin!.Value.Date - periodo.FechaInicio.Date).Days + 1;
	#endregion

	public PlazoLegal PlazoLegal =>
		PlazoLegal.De(Articulo);

	/// <summary>Días de licencia, contando el de inicio y el de fin. Cero si es indefinida.</summary>
	public int Dias =>
		DiasDe(Periodo);

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

		var dias = DiasDe(periodo);

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

	/// <summary>
	/// Aprobada y con la fecha de fin ya pasada, pero sin finalizar: el cierre es manual y la
	/// licencia sigue <see cref="Estado.Activa"/> hasta que alguien registra el reintegro.
	/// </summary>
	public bool EstaVencida() =>
		Estado.Equals(Estado.Activa) && Periodo.HaFinalizado();

	public bool EsIndefinida() =>
		Periodo.EsIndeterminado();

	/// <summary>
	/// Si el período de esta licencia comparte algún día con <paramref name="otro"/>. Los dos
	/// extremos son días de licencia y un período sin fin se extiende indefinidamente.
	/// </summary>
	/// <remarks>
	/// La regla de que un docente no tenga dos licencias en el mismo período compara licencias entre
	/// sí, así que no la puede hacer cumplir el agregado: la aplica <c>Core</c> consultando al
	/// repositorio. Esto es la definición de superposición, para que el repositorio y sus pruebas no
	/// la reescriban cada uno a su manera.
	/// </remarks>
	public bool SeSuperponeCon(RangoFechas otro) =>
		(otro.EsIndeterminado() || Periodo.FechaInicio.Date <= otro.FechaFin!.Value.Date)
		&& (Periodo.EsIndeterminado() || otro.FechaInicio.Date <= Periodo.FechaFin!.Value.Date);

	/// <summary>
	/// Si cuenta para el control de superposición: toda licencia salvo la rechazada, que nunca se
	/// concedió. Las pendientes cuentan, así dos solicitudes para el mismo período chocan al cargarse.
	/// </summary>
	public bool OcupaSuPeriodo() =>
		!Estado.Equals(Estado.Rechazada);

	// Las operaciones sobre una licencia aprobada piden el ESTADO y no la vigencia: con el cierre
	// manual, una licencia vencida y sin finalizar todavía se prorroga o se cierra. Exigir
	// EstaActiva() la dejaba sin salida desde el día siguiente a su fecha de fin.
	private void ValidarAprobada()
	{
		if (!Estado.Equals(Estado.Activa))
		{
			throw new LicenciaInactivaException();
		}
	}

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

	/// <summary>
	/// Rechaza la solicitud: no se concede. Es la otra cara de <see cref="AprobarLicencia"/> y no una
	/// cancelación, que supondría una licencia ya concedida.
	/// </summary>
	public void RechazarLicencia(string? observacion)
	{
		if (!Estado.Equals(Estado.Pendiente))
		{
			throw new LicenciaNoPendienteException();
		}

		Estado = Estado.Rechazada;
		ModificarObservaciones(observacion);

		AgregarEvento(new LicenciaRechazadaEvent(Id, DocenteID));
	}

	/// <summary>
	/// Registra el reintegro del docente. Si la licencia ya venció conserva su fecha de fin, que es
	/// la real; si se cierra antes (regreso anticipado o licencia indefinida), hoy pasa a ser su
	/// último día de licencia.
	/// </summary>
	/// <remarks>
	/// Antes exigía que estuviera vigente y siempre ponía la fecha de hoy: una licencia vencida no se
	/// podía cerrar, y si se hubiera podido, el legajo habría registrado los días que pasaron entre el
	/// vencimiento y el cierre como días de licencia.
	/// </remarks>
	public void FinalizarLicencia()
	{
		ValidarAprobada();

		Estado = Estado.Finalizada;

		if (!Periodo.HaFinalizado())
		{
			Periodo = Periodo.ActualizarFechaFin(Reloj.Hoy);
		}

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

		ValidarAprobada();

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
		ValidarAprobada();

		if (EsIndefinida())
		{
			throw new LicenciaIndefinidaException();
		}

		var nuevoPeriodo = RangoFechas.Create(Periodo.FechaInicio);
		ValidarPlazo(Articulo, nuevoPeriodo);

		Periodo = nuevoPeriodo;
	}

	/// <summary>
	/// Corrige la solicitud mientras está pendiente: artículo, inicio y días se cambian juntos porque
	/// el plazo válido depende del artículo.
	/// </summary>
	/// <remarks>
	/// Reemplaza a <c>ModificarPeriodo</c>, que no recibía el artículo: la pantalla de edición lo
	/// dejaba elegir y el cambio se descartaba en silencio.
	/// <para>
	/// Exige Pendiente, como <see cref="ValidarSePuedeEliminar"/>. Antes rechazaba sólo la licencia
	/// vigente, así que una rechazada, una finalizada o una aprobada ya vencida se podían reescribir,
	/// y con ellas un antecedente del legajo.
	/// </para>
	/// </remarks>
	public void ModificarSolicitud(string articulo, DateTime fechaInicio, int cantidadDias)
	{
		if (!Estado.Equals(Estado.Pendiente))
		{
			throw new LicenciaNoPendienteException();
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

		ValidarAprobada();

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