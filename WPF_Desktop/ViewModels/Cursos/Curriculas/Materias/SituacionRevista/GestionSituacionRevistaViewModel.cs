using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioCatedras;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes;
using Core.Shared.DTOs.Personas.Requests;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Docentes;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;

internal partial class GestionSituacionRevistaViewModel : ObservableValidator, IUbicacion
{
	#region Services
	private readonly INavigationService _gestionCatedrasNavigationService;
	private readonly INavigationService _buscarDocenteModalNavigationService;
	private readonly IServicioDocente _servicioDocente;
	private readonly IServicioCatedra _servicioCatedra;
	private readonly IDialogService _dialogService;
	#endregion

	#region Mensajeria
	private readonly IMessenger _messenger;
	#endregion

	#region Store
	private readonly CursoStore _cursoStore = null;

	private readonly MateriaStore _materiaStore = null;

	private readonly CatedraStore _catedraStore = null;

	private readonly BuscarContextoStore _buscarContextoStore = null;
	#endregion

	#region ViewModels
	[ObservableProperty]
	private MateriaSeleccionada _materia;

	[NotifyCanExecuteChangedFor(nameof(SeleccionarCommand))]
	[ObservableProperty]
	private LegajoDocenteViewModel _legajoDocente;

	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	[ObservableProperty]
	private SituacionRevistaViewModel _situacionRevistaINSERT;

	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	[ObservableProperty]
	private SituacionRevistaViewModel _situacionRevistaUPDATE;
	#endregion

	#region NOTIFICATIONs
	[ObservableProperty]
	private bool _habilitarNotificacion;

	[ObservableProperty]
	private string _mensaje = string.Empty;
	#endregion

	[ObservableProperty]
	private bool _habilitarDocenteEnFunciones = false;

	/// <summary>Situación de revista en funciones de la cátedra (<c>null</c> si no hay ninguna). Se recalcula en cada carga del listado.</summary>
	[ObservableProperty]
	private SituacionRevistaViewModel _situacionRevistaEnFunciones;

	[ObservableProperty]
	private bool _habilitarNuevaSituacionRevista;

	[ObservableProperty]
	private bool _habilitarResultadoBuscar;

	[ObservableProperty]
	private bool _habilitarGestionSituacionRevista;

	[ObservableProperty]
	private bool _habilitarInfoSituacionRevista;

	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	[ObservableProperty]
	private bool _habilitarInsert;

	/// <summary>
	/// <c>true</c> cuando la nueva situación de revista tiene el cargo <c>Suplente</c>: el formulario tiene que pedir
	/// a quién reemplaza (<see cref="SituacionRevistaINSERT"/>.<c>ReemplazaA</c>, elegido entre <see cref="SituacionesReemplazables"/>).
	/// </summary>
	[ObservableProperty]
	private bool _habilitarReemplazaA;

	[ObservableProperty]
	private ObservableCollection<SituacionRevistaViewModel> _docentesEnMateria = new();

	/// <summary>Situaciones de revista vigentes de la cátedra: los candidatos a ser reemplazados por un suplente.</summary>
	[ObservableProperty]
	private ObservableCollection<SituacionRevistaViewModel> _situacionesReemplazables = new();

	#region Censo de la cátedra
	/// <summary>Cuántas designaciones de la cátedra están vigentes.</summary>
	/// <remarks>
	/// Las dos cifras salen de <see cref="DocentesEnMateria"/>, que ya está en memoria: no hay consulta de más.
	/// Contestan de un vistazo lo que la grilla obliga a contar fila por fila desde que es también el historial
	/// —cuántos cargos están vivos y cuánto de lo que se ve es historia—, y la vigencia es la que calcula el
	/// dominio en <c>EsCargoVigente()</c>, no una comparación contra el texto del estado.
	/// </remarks>
	[ObservableProperty]
	private int _designacionesVigentes;

	/// <summary>Cuántas designaciones de la cátedra ya no están vigentes.</summary>
	[ObservableProperty]
	private int _designacionesHistoricas;
	#endregion

	#region Pasar a interino
	/// <summary>Hasta cuándo va a durar la designación que está por pasar a interina.</summary>
	/// <remarks>
	/// EL COMANDO MANDABA <c>DateTime.Today</c>. Un fin que cae hoy no agenda ningún término: cierra
	/// la designación en el acto, que es lo que hace "Finalizar designación". Un cargo interino es un
	/// cargo A TÉRMINO, y el término hay que elegirlo, así que ahora lo pide la pantalla y el dominio
	/// lo exige posterior a hoy.
	///
	/// Se vacía con cada fila elegida: el término es de ESTA designación y no del formulario.
	/// </remarks>
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[ObservableProperty]
	private DateTime? _terminoDeLaDesignacion;

	/// <summary>Primer día elegible como término: mañana, o el inicio si la designación no empezó.</summary>
	/// <remarks>
	/// LAS DOS COTAS SON DEL DOMINIO, no de la vista. <c>SituacionRevista.PasarAInterinoHasta</c> pide
	/// un fin posterior a hoy, y <c>RangoFechas</c> rechaza un fin anterior al inicio con
	/// <c>FechasInconsistentesException</c>. El calendario deja de ofrecer lo que iba a reventar.
	/// </remarks>
	public DateTime PrimerTerminoPosible
	{
		get
		{
			var manana = DateTime.Today.AddDays(1);
			var inicio = SituacionRevistaUPDATE?.FechaInicio ?? manana;

			return inicio > manana ? inicio : manana;
		}
	}

	/// <summary>
	/// <c>true</c> cuando la designación elegida admite término. Dibuja el campo de fecha; el botón
	/// espera además a que la fecha esté puesta.
	/// </summary>
	/// <remarks>
	/// Vigente y sin fecha de fin es, en los hechos, un titular: el interino y el suplente nacen con
	/// término —<c>CargoTemporalSinFechaFinalizacionException</c>— y el dominio los rechaza acá con
	/// <c>CargoConFechaFinalizacionException</c>.
	/// </remarks>
	public bool PuedePasarAInterino =>
		SituacionRevistaUPDATE is not null && SituacionRevistaUPDATE.Vigente && SituacionRevistaUPDATE.FechaFin is null;
	#endregion

	#region Commands
	public IAsyncRelayCommand CargarSituacionRevistasCommandAsync { get; }
	public IAsyncRelayCommand CancelarCommand { get; }
	public IAsyncRelayCommand EditarCommand { get; }
	public IAsyncRelayCommand EliminarCommand { get; }
	public IAsyncRelayCommand GuardarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	public IRelayCommand RegistrarCommand { get; }
	public IRelayCommand SeleccionarCommand { get; }
	public IRelayCommand BuscarDocenteCommand { get; }
	#endregion


	public GestionSituacionRevistaViewModel(INavigationService gestionCatedrasNavigationService,
											INavigationService buscarDocenteModalNavigationService,
											IServicioDocente servicioDocente,
											IServicioCatedra servicioCatedra,
											IDialogService dialogService,
											CursoStore cursoStore,
											MateriaStore materiaStore,
											CatedraStore catedraStore,
											BuscarContextoStore buscarContextoStore,
											IMessenger messenger)
	{
		_gestionCatedrasNavigationService = gestionCatedrasNavigationService;
		_buscarDocenteModalNavigationService = buscarDocenteModalNavigationService;
		_servicioDocente = servicioDocente;
		_servicioCatedra = servicioCatedra;
		_dialogService = dialogService;
		_messenger = messenger;

		_cursoStore = cursoStore;
		_materiaStore = materiaStore;
		_catedraStore = catedraStore;
		_buscarContextoStore = buscarContextoStore;

		CargarSituacionRevistasCommandAsync = new AsyncRelayCommand(CargarSituacionRevistasAsync);
		CancelarCommand = new AsyncRelayCommand<string>(ExecuteCancelarCommandAsync, CanExecuteCancelarCommand);
		EditarCommand = new AsyncRelayCommand<string>(ExecuteEditarCommandAsync, CanExecuteEditarCommand);
		EliminarCommand = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		GuardarCommand = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);
		RegistrarCommand = new RelayCommand<string>(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
		SeleccionarCommand = new RelayCommand(ExecuteSeleccionarCommand, CanExecuteCommand);
		BuscarDocenteCommand = new RelayCommand(ExecuteBuscarDocenteCommand, () => HabilitarInsert);

		// El buscador vive en el modal y no conoce a sus llamadores: el elegido vuelve por acá.
		_messenger.Register<GestionSituacionRevistaViewModel, DocenteElegidoMessage>(this, (receptor, mensaje) =>
		{
			receptor.LegajoDocente = mensaje.Docente;
			receptor.SeleccionarCommand.Execute(null);
		});

		// Va después de los comandos: el setter generado notifica a los comandos que declaran [NotifyCanExecuteChangedFor].
		Materia = _materiaStore.Materia;

		// Sin parámetro, a diferencia de NavigationCommand: la miga ejecuta el comando con null
		// y el switch por string no haría nada.
		VolverCommand = new RelayCommand(gestionCatedrasNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Contexto("CURSOS"),
			Migaja.Contexto("DISEÑO CURRICULAR"),
			Migaja.Hacia($"{ Materia?.Descripcion?.ToUpper() } · CÁTEDRAS", VolverCommand),
			Migaja.Actual("DOCENTES")
		};
	}

	/// <summary>
	/// Ruta hasta los docentes de la cátedra. La materia era el título de la pantalla —el mismo
	/// que en Cátedras y en Horarios—, así que las tres se llamaban igual.
	/// </summary>
	/// <remarks>
	/// LA MIGA DEL MEDIO DECÍA UN DESTINO Y LLEVABA A OTRO. Era <c>Hacia("DISEÑO CURRICULAR")</c>
	/// colgada de <see cref="VolverCommand"/>, pero el servicio de navegación que la fábrica le pasa
	/// a esta pantalla es el de CÁTEDRAS —<c>CreateGestionCatedrasNavigationService</c>—, así que el
	/// clic aterrizaba un nivel más abajo del que prometía el texto. Ahora tiene la forma de su
	/// pantalla hermana, Horarios: el diseño curricular es un tramo de contexto, al que desde acá no
	/// hay salto directo, y el enlace es el nivel del que efectivamente se viene.
	/// </remarks>
	public IReadOnlyList<Migaja> Ruta { get; }

	/// <summary>Vuelve a las cátedras de la materia. Es el destino del viejo botón "Volver a materias".</summary>
	public IRelayCommand VolverCommand { get; }

	/// <summary>
	/// La designación elegida admite al menos una de las cuatro acciones del pie.
	/// </summary>
	/// <remarks>
	/// EL PIE SE DIBUJA SÓLO SI TIENE ALGO ADENTRO. Cada botón cuelga su <c>Visibility</c> del
	/// <c>IsEnabled</c> que su propio <c>CanExecute</c> decide, así que sobre una designación cerrada
	/// —ni vigente ni en funciones— los cuatro se colapsan y el <c>HeaderedContentControl</c> seguía
	/// poniendo igual su regla de 1px y sus 36px de margen: una línea huérfana bajo el detalle, sin
	/// nada que explicara por qué no había acciones.
	///
	/// Se pregunta por los mismos <c>CanExecute</c> en lugar de reimplementar las cuatro condiciones:
	/// la regla sigue viviendo en un solo lugar, medida contra el dominio.
	/// </remarks>
	/// <remarks>
	/// Para el pase a interino se pregunta por <see cref="PuedePasarAInterino"/> y no por el
	/// <c>CanExecute</c> del comando: ese espera además a que el término esté elegido, y el campo
	/// donde se lo elige vive en este mismo pie. Colgarlo del comando lo escondía justo cuando hacía
	/// falta, y el botón no aparecía nunca.
	/// </remarks>
	public bool HayAccionesSobreLaDesignacion =>
		EliminarCommand.CanExecute("Docente")
	 || PuedePasarAInterino
	 || EditarCommand.CanExecute("Docente")
	 || GuardarCommand.CanExecute("Docente");

	partial void OnSituacionRevistaUPDATEChanged(SituacionRevistaViewModel value)
	{
		// El término es de la designación elegida: cambiar de fila lo descarta.
		TerminoDeLaDesignacion = null;

		OnPropertyChanged(nameof(PuedePasarAInterino));
		OnPropertyChanged(nameof(PrimerTerminoPosible));
		OnPropertyChanged(nameof(HayAccionesSobreLaDesignacion));
	}

	#region BuscarDocenteCommand
	/// <summary>
	/// Abre el buscador de docentes en el modal. Esta pantalla traía su propia copia del buscador
	/// —campo validado, lupa, un Expander con la grilla de resultados y un botón "Seleccionar"—,
	/// idéntica a la de <c>GestionDivisionesView</c>. Las dos se reemplazaron por el modal.
	/// </summary>
	/// <remarks>
	/// Declara su contexto antes de navegar, igual que Divisiones, pero el suyo dice "Elegir
	/// docente" y no un verbo que cierre: acá el docente es el PRIMER dato de la designación y
	/// después hay cargo, período y —si es suplente— a quién reemplaza. El modal es un paso del
	/// formulario, no el formulario entero. Ver <see cref="BuscarContexto"/>.
	/// </remarks>
	private void ExecuteBuscarDocenteCommand()
	{
		_buscarContextoStore.Declarar(new BuscarContexto(
			Titulo: $"Docente para { Materia?.Descripcion ?? "la cátedra" }",
			Detalle: "Elegilo de la lista y después cargá su cargo y su período en la cátedra.",
			Verbo: "Elegir docente"));

		_buscarDocenteModalNavigationService.Navigate();
	}
	#endregion

	public async Task CargarSituacionRevistasAsync()
	{
		try
		{
			DocentesEnMateria.Clear();
			SituacionesReemplazables.Clear();
			SituacionRevistaEnFunciones = null;
			HabilitarDocenteEnFunciones = false;
			DesignacionesVigentes = 0;
			DesignacionesHistoricas = 0;

			var request = new ListarSituacionesRevistaRequest(CatedraID: _catedraStore.Catedra);
			var situaciones = await _servicioCatedra.ListarSituacionesRevistaAsync(request);

			// SIN EL RETORNO TEMPRANO. Una cátedra sin designaciones apagaba la tarjeta entera y lo
			// avisaba con un cartel interruptor, de modo que el estado vacío de la grilla —que ya
			// trae su frase y dice qué hacer— era código inalcanzable. Es el caso normal de una
			// cátedra recién creada, no un problema que merezca interrumpir.
			var nombresPorDocente = await NombresDeLosDocentesAsync(situaciones.Select(x => x.DocenteID));

			var items = situaciones.Select(x => new SituacionRevistaViewModel(x)
			{
				Docente = Nombrar(nombresPorDocente, x.DocenteID),
				ReemplazaADocente = x.ReemplazaADocenteID.HasValue ? Nombrar(nombresPorDocente, x.ReemplazaADocenteID.Value) : string.Empty
			});
			DocentesEnMateria = new ObservableCollection<SituacionRevistaViewModel>(items);

			// El censo de la ficha, sobre la lista que ya está en memoria.
			DesignacionesVigentes = DocentesEnMateria.Count(situacion => situacion.Vigente);
			DesignacionesHistoricas = DocentesEnMateria.Count - DesignacionesVigentes;

			// Candidatos a ser reemplazados: vigentes Y sin suplente vigente encima. La segunda
			// condición es la que faltaba, y es la misma con la que el dominio rechaza una segunda
			// suplencia sobre el mismo ausente (DocenteYaReemplazadoException). El combo ofrecía
			// activamente la opción que Core iba a rechazar.
			SituacionesReemplazables = new ObservableCollection<SituacionRevistaViewModel>(
				DocentesEnMateria.Where(situacion => situacion.Vigente && !situacion.TieneSuplenteVigente));

			// El bloque "Docente en Aula" se muestra sólo si hay alguien en funciones, y apunta a esa situación.
			SituacionRevistaEnFunciones = DocentesEnMateria.FirstOrDefault(x => x.EnFunciones);
			HabilitarDocenteEnFunciones = SituacionRevistaEnFunciones is not null;
			SituacionRevistaUPDATE = SituacionRevistaEnFunciones;

			HabilitarNotificacion = false;
			HabilitarGestionSituacionRevista = true;
			HabilitarInfoSituacionRevista = true;
		}
		catch (Exception ex)
		{
			// Un fallo de carga es un error y no una advertencia: es la única pista de por qué la
			// grilla está vacía.
			_dialogService.MostrarError($"No se pudieron traer los docentes de la cátedra. { ex.Message }", "Docentes de la cátedra");

			HabilitarNotificacion = false;
			HabilitarGestionSituacionRevista = true;
			HabilitarInfoSituacionRevista = true;
		}
	}

	#region Nombres de los docentes
	/// <summary>
	/// Resuelve los nombres de los docentes que aparecen en el listado.
	/// </summary>
	/// <remarks>
	/// <c>SituacionRevistaResponse</c> identifica a cada docente por su identificador, así que el
	/// nombre se cruza acá. Arranca por el listado de activos —una sola consulta para todos— y recién
	/// después pide de a uno los que falten.
	///
	/// Esos que faltan son los docentes DADOS DE BAJA, y son justamente los que el historial existe
	/// para mostrar: antes salían como "Docente no disponible", así que la designación de alguien que
	/// ya no trabaja en la escuela perdía el nombre de la persona. Son pocos y sólo se los pide cuando
	/// aparecen, de modo que el caso normal sigue costando una consulta.
	/// </remarks>
	private async Task<Dictionary<Guid, string>> NombresDeLosDocentesAsync(IEnumerable<Guid> docentesID)
	{
		var nombres = new Dictionary<Guid, string>();

		var buscados = docentesID.Distinct().ToList();

		// Una cátedra sin designaciones no tiene nombres que resolver: no se consulta el listado.
		if (buscados.Count is 0)
		{
			return nombres;
		}

		var activos = await _servicioDocente.ListarDocentesActivosAsync();
		foreach (var docente in activos)
		{
			nombres.TryAdd(docente.DocenteID, docente.NombreCompleto);
		}

		foreach (var docenteID in buscados.Where(id => !nombres.ContainsKey(id)))
		{
			try
			{
				var legajo = await _servicioDocente.MostrarLegajoDocenteAsync(new DocenteIDRequest(docenteID));
				if (legajo is not null)
				{
					nombres.TryAdd(docenteID, legajo.NombreCompleto);
				}
			}
			catch (Exception)
			{
				// Un legajo que no se puede traer no invalida el resto del listado: esa fila cae en
				// el texto de reemplazo y las demás se muestran igual.
			}
		}

		return nombres;
	}

	private static string Nombrar(Dictionary<Guid, string> nombres, Guid docenteID) =>
		nombres.TryGetValue(docenteID, out var nombre) ? nombre : "Docente sin legajo";
	#endregion

	#region Suplencia

	partial void OnSituacionRevistaINSERTChanged(SituacionRevistaViewModel oldValue, SituacionRevistaViewModel newValue)
	{
		if (oldValue is not null)
		{
			oldValue.PropertyChanged -= OnSituacionRevistaINSERTPropertyChanged;
		}

		if (newValue is not null)
		{
			newValue.PropertyChanged += OnSituacionRevistaINSERTPropertyChanged;
		}

		ActualizarReemplazaA();
	}

	/// <remarks>
	/// El formulario del alta es OTRO ViewModel, así que sus cambios no reevalúan solos el
	/// <c>CanExecute</c> de esta pantalla. Sin esto, completar la fecha de fin que falta no encendía
	/// el botón de guardar hasta que algo más lo notificara.
	/// </remarks>
	private void OnSituacionRevistaINSERTPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(SituacionRevistaViewModel.Cargo))
		{
			ActualizarReemplazaA();
		}

		GuardarCommand.NotifyCanExecuteChanged();
	}

	private void ActualizarReemplazaA()
	{
		HabilitarReemplazaA = SituacionRevistaINSERT is not null && SituacionRevistaINSERT.Cargo.Equals("Suplente");

		if (!HabilitarReemplazaA && SituacionRevistaINSERT is not null)
		{
			SituacionRevistaINSERT.ReemplazaA = null;
		}
	}
	#endregion

	#region Commands
	#region CancelarCommand
	private bool CanExecuteCancelarCommand(object obj) => obj switch
	{
		"Insert" => SituacionRevistaINSERT is not null,
		_ => false
	};

	private async Task ExecuteCancelarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Insert":
				await CargarSituacionRevistasAsync();
				HabilitarNuevaSituacionRevista = false;
				HabilitarInsert = false;

				break;
		}
	}
	#endregion

	#region EditarCommand
	/// <remarks>
	/// LOS CUATRO GATES SE MIDEN CONTRA EL DOMINIO Y NO CONTRA EL TEXTO DEL ESTADO. Comparar
	/// <c>Estado</c> con la cadena "Finalizado" no es lo mismo que preguntar si la designación está
	/// vigente: una aceptada cuyo período ya terminó sigue diciendo "Aceptado". Ahora se usa
	/// <c>Vigente</c>, que viene calculado de <c>SituacionRevista.EsCargoVigente()</c>.
	///
	/// "Interino" exige además que NO tenga fecha de fin. Llama a <c>PasarAInterinoHasta</c>, que
	/// rechaza toda designación temporal con <c>CargoConFechaFinalizacionException</c>, y como el
	/// suplente y el interino siempre llevan fecha de fin, para esas dos filas el botón estaba
	/// encendido y garantizado a fallar: sólo sirve sobre un titular.
	///
	/// Y exige el término elegido, porque el dominio lo pide posterior a hoy: sin esa condición el
	/// botón abría su confirmación para después reventar con un <c>ArgumentException</c>.
	/// </remarks>
	private bool CanExecuteEditarCommand(object obj) => obj switch
	{
		// Quitar del aula: el dominio sólo pide que haya alguien en funciones.
		"Docente" => SituacionRevistaUPDATE is not null && SituacionRevistaUPDATE.EnFunciones,
		"Interino" => PuedePasarAInterino && TerminoDeLaDesignacion.HasValue,
		_ => false
	};

	private async Task ExecuteEditarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Docente":
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Quitar del aula a { SituacionRevistaUPDATE.Docente }",
						consecuencia: $"La cátedra queda sin docente en funciones y { SituacionRevistaUPDATE.Docente } deja de figurar "
									+ $"frente al curso. El cargo de { SituacionRevistaUPDATE.Cargo.ToLower() } sigue siendo suyo: "
									+ "para liberarlo hay que poner fin a la designación.",
						verbo: "Quitar del aula")))
				{
					try
					{
						var request = new RelevarDeFuncionesRequest(CatedraID: _catedraStore.Catedra);
						await _servicioCatedra.RelevarDeFuncionesAsync(request);

						_dialogService.MostrarInformacion($"{ SituacionRevistaUPDATE.Docente } ya no está en funciones.", "Docente relevado");
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "No se pudo quitar del aula al docente");
					}
				}

				await CargarSituacionRevistasAsync();
				break;

			case "Interino":
				// EL CUERPO NO CONFÍA EN EL GATE. AsyncRelayCommand.ExecuteAsync no consulta
				// CanExecute: el botón de la pantalla sí, pero cualquier otro llamador —una prueba,
				// un atajo, el día que esto se dispare desde otro lado— entraba acá sin término y se
				// iba en un NullReferenceException sobre .Value.
				if (!PuedePasarAInterino || !TerminoDeLaDesignacion.HasValue)
				{
					break;
				}

				// No es destructiva: no se pierde nada, el cargo pasa a tener término. Revisar es la
				// forma que corresponde —un dato que cambia el expediente del docente—, y además el
				// rojo estaba diciendo "esto no se puede deshacer" sobre algo que sí se puede volver
				// a designar.
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Pasar a interino a { SituacionRevistaUPDATE.Docente }",
						mensaje: $"La designación de { SituacionRevistaUPDATE.Cargo.ToLower() }, vigente desde el "
							   + $"{ SituacionRevistaUPDATE.FechaInicio.ToString("D") }, pasa a ser un cargo interino "
							   + $"que vence el { TerminoDeLaDesignacion.Value.ToString("D") }. "
							   + "Hasta esa fecha el docente sigue en el cargo; después, la cátedra queda libre para designar a otro.",
						verbo: "Pasar a interino")))
				{
					// El término se lee ANTES de recargar: CargarSituacionRevistasAsync reapunta
					// SituacionRevistaUPDATE y, con él, vacía TerminoDeLaDesignacion.
					var termino = TerminoDeLaDesignacion.Value;
					var docenteInterino = SituacionRevistaUPDATE.Docente;

					try
					{
						var request = new PasarAInterinoRequest(CatedraID: _catedraStore.Catedra,
																SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID,
																FechaFin: termino);
						await _servicioCatedra.PasarAInterinoAsync(request);

						_dialogService.MostrarInformacion($"{ docenteInterino } queda como interino hasta el { termino.ToString("d") }.",
														  "Designación a término");
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "No se pudo pasar la designación a interina");
					}
				}

				// Una sola recarga, al salir. Antes había dos en el camino feliz.
				await CargarSituacionRevistasAsync();
				break;
		}
	}
	#endregion

	#region EliminarCommand
	/// <remarks>
	/// Exige <c>Vigente</c>: <c>SituacionRevista.Finalizar()</c> rechaza una designación que no empezó
	/// —<c>CargoNoIniciadoException</c>— y una que ya terminó —<c>CargoNoVigenteException</c>—, y el
	/// gate sólo pedía que hubiera una fila elegida. Sobre una designación ya finalizada el botón
	/// quedaba vivo, abría su confirmación destructiva y recién después reventaba.
	/// </remarks>
	private bool CanExecuteEliminarCommand(object obj) => obj switch
	{
		"Docente" => SituacionRevistaUPDATE is not null && SituacionRevistaUPDATE.Vigente,
		_ => false
	};

	private async Task ExecuteEliminarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Docente":
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Finalizar la designación de { SituacionRevistaUPDATE.Docente }",
						// NO DICE QUE LAS HORAS VUELVEN A ESTAR SIN ASIGNAR, que es lo que decía y era falso.
						// Catedra.HorasAsignadas es _horarios.Count y HorasSinAsignar es
						// CargaHoraria - HorasAsignadas: lo que se asigna ahí es el lugar en el horario
						// semanal, y finalizar una designación no toca ningún horario. Una consecuencia
						// inventada en un diálogo destructivo es peor que ninguna.
						consecuencia: $"La designación vigente desde el { SituacionRevistaUPDATE.FechaInicio.ToString("D") } queda finalizada "
									+ "y el cargo deja de estar vigente en la cátedra, así que se puede designar a otro docente en su lugar. "
									+ "Los horarios de la cátedra no cambian.",
						verbo: "Finalizar designación")))
				{
					// El nombre se lee ANTES de recargar: la recarga reapunta SituacionRevistaUPDATE a
					// quien quedó en funciones, y el cartel terminaba diciendo que el docente que
					// sigue en el aula ya no está designado.
					var docenteFinalizado = SituacionRevistaUPDATE.Docente;

					try
					{
						var request = new FinalizarDesignacionRequest(CatedraID: _catedraStore.Catedra,
																	  SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID);
						await _servicioCatedra.FinalizarDesignacionAsync(request);

						_dialogService.MostrarInformacion($"{ docenteFinalizado } ya no está designado en la cátedra.", "Designación finalizada");
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "No se pudo finalizar la designación");
					}

					// Dentro del if: cancelar el diálogo no tiene por qué mover la fila que el
					// usuario tenía elegida.
					await CargarSituacionRevistasAsync();
				}

				break;
		}
	}
	#endregion

	#region GuardarCommand
	/// <remarks>
	/// <para>
	/// "Docente" (poner en aula) EXCLUÍA AL SUPLENTE, y eso no es una regla del dominio:
	/// <c>Catedra.PonerEnFunciones</c> acepta cualquier designación vigente. Un suplente existe
	/// justamente para ponerse al frente del curso mientras dura la ausencia del titular, así que la
	/// pantalla le prohibía lo único para lo que se lo designa. Lo que sí hay que exigir —y faltaba—
	/// es que no esté YA en funciones, porque el dominio lo rechaza con
	/// <c>DocenteEnFuncionesException</c>.
	/// </para>
	/// <para>
	/// "Insert" exige la fecha de fin cuando el cargo la necesita. El dominio rechaza un interino o un
	/// suplente sin ella con <c>CargoTemporalSinFechaFinalizacionException</c>, y el ViewModel lo venía
	/// esquivando de la peor manera: le ponía como fecha de fin la de inicio, o sea una designación de
	/// un solo día que nadie pidió ni vio.
	/// </para>
	/// </remarks>
	private bool CanExecuteGuardarCommand(object obj) => obj switch
	{
		"Insert" => SituacionRevistaINSERT is not null
				 && !SituacionRevistaINSERT.HasErrors
				 && (!SituacionRevistaINSERT.ExigeFechaFin || SituacionRevistaINSERT.FechaFin is not null)
				 && (!SituacionRevistaINSERT.ExigeReemplazo || SituacionRevistaINSERT.ReemplazaA is not null),
		"Docente" => SituacionRevistaUPDATE is not null && SituacionRevistaUPDATE.Vigente && !SituacionRevistaUPDATE.EnFunciones,
		_ => false
	};

	private async Task ExecuteGuardarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Docente":
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Poner en aula a { SituacionRevistaUPDATE.Docente }",
						mensaje: $"Pasa a estar frente al curso en { _materiaStore.Materia.Descripcion } con el cargo de "
							   + $"{ SituacionRevistaUPDATE.Cargo.ToLower() }. Si ya había alguien en funciones, lo reemplaza.",
						verbo: "Poner en aula")))
				{
					try
					{
						var request = new PonerEnFuncionesRequest(CatedraID: _catedraStore.Catedra,
																  SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID);
						await _servicioCatedra.PonerEnFuncionesAsync(request);

						_dialogService.MostrarInformacion($"{ SituacionRevistaUPDATE.Docente } está frente al curso.", "Docente en aula");
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "No se pudo poner en aula al docente");
					}
				}

				await CargarSituacionRevistasAsync();
				break;

			case "Insert":
				if (HabilitarReemplazaA && SituacionRevistaINSERT.ReemplazaA is null)
				{
					_dialogService.MostrarAdvertencia("Elegí a quién reemplaza el docente suplente.", "Falta a quién reemplaza");
					return;
				}

				// El diálogo nombra TODO lo que la operación va a hacer, incluido el paso al aula.
				// Antes la designación ponía al docente frente al curso por su cuenta —el formulario
				// nacía con "en aula" prendido y la casilla deshabilitada— y la confirmación no lo
				// mencionaba: el usuario aceptaba una cosa y pasaban dos.
				// EL TÉRMINO SÓLO EXISTE PARA EL CARGO QUE LO ADMITE. El dominio rechaza un titular
				// con fecha de fin —TitularConFechaFinalizacionException—, y el campo ya no se
				// dibuja para ese cargo, pero el valor puede haber quedado de un cargo anterior.
				// Mismo criterio que ReemplazaA, que se limpia acá abajo por la misma razón: lo que
				// el formulario no está mostrando no viaja.
				var terminoDelAlta = SituacionRevistaINSERT.ExigeFechaFin ? SituacionRevistaINSERT.FechaFin : null;

				var hastaCuando = terminoDelAlta.HasValue
					? $" hasta el { terminoDelAlta.Value.ToString("D") }"
					: " por tiempo indeterminado";

				var tambienAlAula = SituacionRevistaINSERT.EnFunciones
					? " Además queda frente al curso desde ya, en lugar de quien estuviera en el aula."
					: " No queda frente al curso: para eso está “Poner en aula”.";

				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Designar a { SituacionRevistaINSERT.Docente } en { _materiaStore.Materia.Descripcion }",
						mensaje: $"Cargo: { SituacionRevistaINSERT.Cargo }. Desde el { SituacionRevistaINSERT.FechaInicio.ToString("D") }{ hastaCuando }. "
							   + "La designación ocupa horas de la cátedra."
							   + tambienAlAula,
						verbo: "Designar docente")))
				{
					try
					{
						// Sólo el suplente reemplaza a alguien: para titular e interino la cadena de suplencias no aplica.
						var request = new DesignarDocenteRequest(CatedraID: _catedraStore.Catedra,
																 DocenteID: SituacionRevistaINSERT.DocenteID,
																 Cargo: SituacionRevistaINSERT.Cargo,
																 FechaInicio: SituacionRevistaINSERT.FechaInicio,
																 FechaFin: terminoDelAlta,
																 ReemplazaA: HabilitarReemplazaA ? SituacionRevistaINSERT.ReemplazaA : null);
						var situacionRevistaID = await _servicioCatedra.DesignarDocenteAsync(request);

						if (SituacionRevistaINSERT.EnFunciones)
						{
							await _servicioCatedra.PonerEnFuncionesAsync(new PonerEnFuncionesRequest(CatedraID: _catedraStore.Catedra,
																									 SituacionRevistaID: situacionRevistaID));
						}

						_dialogService.MostrarInformacion($"{ SituacionRevistaINSERT.Docente } como { SituacionRevistaINSERT.Cargo.ToLower() }.", "Docente designado");
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "No se pudo designar el docente");
					}
					finally
					{
						HabilitarInsert = false;
						HabilitarNuevaSituacionRevista = false;
					}

					await CargarSituacionRevistasAsync();
				}
				break;
		}
	}
	#endregion

	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj) => obj switch
	{
		"Materias" => !HabilitarInsert,
		_ => false
	};

	private void ExecuteNavigationCommand(object obj)
	{
		switch (obj)
		{
			case "Materias":
				_gestionCatedrasNavigationService.Navigate();
				break;
		}
	}
	#endregion

	#region RegistrarCommand
	private bool CanExecuteRegistrarCommand(object obj) => obj switch
	{
		"SituacionRevista" => !HabilitarInsert,
		_ => false
	};

	private void ExecuteRegistrarCommand(object obj)
	{
		switch (obj)
		{
			case "SituacionRevista":
				HabilitarNotificacion = false;
				HabilitarGestionSituacionRevista = true;
				HabilitarInfoSituacionRevista = false;
				HabilitarInsert = true;
				// Abre el buscador en el modal, que es de donde sale el docente a designar.
				ExecuteBuscarDocenteCommand();
				break;
		}
	}
	#endregion

	#region SeleccionarCommand
	private bool CanExecuteCommand() =>
		LegajoDocente is not null;

	private void ExecuteSeleccionarCommand()
	{
		HabilitarNuevaSituacionRevista = true;

		SituacionRevistaINSERT = new(docenteID: LegajoDocente.DocenteID, docente: LegajoDocente.NombreCompleto);
	}
	#endregion
	#endregion
}
