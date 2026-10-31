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
	private readonly INavigationService _gestionMateriasNavigationService;
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


	public GestionSituacionRevistaViewModel(INavigationService gestionMateriasNavigationService,
											INavigationService buscarDocenteModalNavigationService,
											IServicioDocente servicioDocente,
											IServicioCatedra servicioCatedra,
											IDialogService dialogService,
											CursoStore cursoStore,
											MateriaStore materiaStore,
											CatedraStore catedraStore,
											IMessenger messenger)
	{
		_gestionMateriasNavigationService = gestionMateriasNavigationService;
		_buscarDocenteModalNavigationService = buscarDocenteModalNavigationService;
		_servicioDocente = servicioDocente;
		_servicioCatedra = servicioCatedra;
		_dialogService = dialogService;
		_messenger = messenger;

		_cursoStore = cursoStore;
		_materiaStore = materiaStore;
		_catedraStore = catedraStore;

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
		VolverCommand = new RelayCommand(gestionMateriasNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Contexto("CURSOS"),
			Migaja.Hacia("DISEÑO CURRICULAR", VolverCommand),
			Migaja.Actual($"{ Materia?.Descripcion?.ToUpper() } · DOCENTES")
		};
	}

	/// <summary>
	/// Ruta hasta los docentes de la cátedra. La materia era el título de la pantalla —el mismo
	/// que en Cátedras y en Horarios—, así que las tres se llamaban igual.
	/// </summary>
	public IReadOnlyList<Migaja> Ruta { get; }

	/// <summary>Vuelve al diseño curricular. Es el destino del viejo botón "Volver a materias".</summary>
	public IRelayCommand VolverCommand { get; }

	#region BuscarDocenteCommand
	/// <summary>
	/// Abre el buscador de docentes en el modal. Esta pantalla traía su propia copia del buscador
	/// —campo validado, lupa, un Expander con la grilla de resultados y un botón "Seleccionar"—,
	/// idéntica a la de <c>GestionDivisionesView</c>. Las dos se reemplazaron por el modal.
	/// </summary>
	private void ExecuteBuscarDocenteCommand() =>
		_buscarDocenteModalNavigationService.Navigate();
	#endregion

	public async Task CargarSituacionRevistasAsync()
	{
		try
		{
			DocentesEnMateria.Clear();
			SituacionesReemplazables.Clear();
			SituacionRevistaEnFunciones = null;
			HabilitarDocenteEnFunciones = false;

			var request = new ListarSituacionesRevistaRequest(CatedraID: _catedraStore.Catedra);
			var situaciones = await _servicioCatedra.ListarSituacionesRevistaAsync(request);
			if (situaciones.Count is 0)
			{
				string messageBoxText = "No existen docentes registrados para esta materia.";
				string caption = "Docentes";
				_dialogService.MostrarAdvertencia(messageBoxText, caption);

				Mensaje = messageBoxText;
				HabilitarNotificacion = true;
				HabilitarGestionSituacionRevista = false;

				return;
			}

			// SituacionRevistaResponse trae DocenteID y no el nombre: una sola consulta y cruce en memoria.
			var docentes = await _servicioDocente.ListarDocentesActivosAsync();
			var nombresPorDocente = new System.Collections.Generic.Dictionary<Guid, string>();
			foreach (var docente in docentes)
			{
				nombresPorDocente.TryAdd(docente.DocenteID, docente.NombreCompleto);
			}

			var items = situaciones.Select(x => new SituacionRevistaViewModel(x)
			{
				Docente = nombresPorDocente.TryGetValue(x.DocenteID, out var nombre) ? nombre : "Docente no disponible"
			});
			DocentesEnMateria = new ObservableCollection<SituacionRevistaViewModel>(items);

			SituacionesReemplazables = new ObservableCollection<SituacionRevistaViewModel>(DocentesEnMateria.Where(EsVigente));

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
			string messageBoxText = $"Error al cargar los docentes de la materia.\nError: {ex.Message}";
			_dialogService.MostrarAdvertencia(messageBoxText, "Error en la operación");

			Mensaje = messageBoxText;
			HabilitarNotificacion = true;
		}
	}

	#region Suplencia
	private static bool EsVigente(SituacionRevistaViewModel situacion) =>
		!situacion.Estado.Equals("Finalizado")
		&& situacion.FechaInicio.Date <= DateTime.Today
		&& (situacion.FechaFin is null || situacion.FechaFin.Value.Date >= DateTime.Today);

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

	private void OnSituacionRevistaINSERTPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(SituacionRevistaViewModel.Cargo))
		{
			ActualizarReemplazaA();
		}
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
	private bool CanExecuteEditarCommand(object obj) => obj switch
	{
		"Docente" => SituacionRevistaUPDATE is not null && !SituacionRevistaUPDATE.Estado.Equals("Finalizado") && SituacionRevistaUPDATE.EnFunciones,
		"Rescindir" => SituacionRevistaUPDATE is not null && !SituacionRevistaUPDATE.Estado.Equals("Finalizado"),
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

			case "Rescindir":
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Poner fin a la designación de { SituacionRevistaUPDATE.Docente }",
						consecuencia: $"La designación de { SituacionRevistaUPDATE.Cargo.ToLower() }, vigente desde el "
									+ $"{ SituacionRevistaUPDATE.FechaInicio.ToString("D") }, se cierra hoy "
									+ $"({ DateTime.Today.ToString("D") }) y el cargo vuelve a estar disponible en la cátedra.",
						verbo: "Poner fin")))
				{
					try
					{
						var request = new EstablecerFinDeDesignacionRequest(CatedraID: _catedraStore.Catedra,
																			SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID,
																			FechaFin: DateTime.Today);
						await _servicioCatedra.EstablecerFinDeDesignacionAsync(request);
						await CargarSituacionRevistasAsync();

						_dialogService.MostrarInformacion($"El cargo de { SituacionRevistaUPDATE.Cargo.ToLower() } vuelve a estar disponible.", "Designación con fecha de fin");
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "No se pudo poner fin a la designación");
					}
				}

				await CargarSituacionRevistasAsync();
				break;
		}
	}
	#endregion

	#region EliminarCommand
	private bool CanExecuteEliminarCommand(object obj) => obj switch
	{
		"Docente" => SituacionRevistaUPDATE is not null,
		_ => false
	};

	private async Task ExecuteEliminarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Docente":
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Finalizar la designación de { SituacionRevistaUPDATE.Docente }",
						consecuencia: $"La designación vigente desde el { SituacionRevistaUPDATE.FechaInicio.ToString("D") } queda finalizada "
									+ "y el cargo deja de estar vigente en la cátedra. Las horas que ocupaba vuelven a estar sin asignar.",
						verbo: "Finalizar designación")))
				{
					try
					{
						var request = new FinalizarDesignacionRequest(CatedraID: _catedraStore.Catedra,
																	  SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID);
						await _servicioCatedra.FinalizarDesignacionAsync(request);
						await CargarSituacionRevistasAsync();

						_dialogService.MostrarInformacion($"{ SituacionRevistaUPDATE.Docente } ya no está designado en la cátedra.", "Designación finalizada");
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "No se pudo finalizar la designación");
					}
				}

				SituacionRevistaUPDATE = SituacionRevistaEnFunciones;
				break;
		}
	}
	#endregion

	#region GuardarCommand
	private bool CanExecuteGuardarCommand(object obj) => obj switch
	{
		"Insert" => SituacionRevistaINSERT is not null,
		"Docente" => SituacionRevistaUPDATE is not null && !SituacionRevistaUPDATE.Estado.Equals("Finalizado") && (SituacionRevistaUPDATE.Cargo.Equals("Titular") || SituacionRevistaUPDATE.Cargo.Equals("Interino")),
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

				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Designar a { SituacionRevistaINSERT.Docente } en { _materiaStore.Materia.Descripcion }",
						mensaje: $"Cargo: { SituacionRevistaINSERT.Cargo }. Desde el { SituacionRevistaINSERT.FechaInicio.ToString("D") }. "
							   + "La designación ocupa horas de la cátedra.",
						verbo: "Designar docente")))
				{
					try
					{
						// Sólo el suplente reemplaza a alguien: para titular e interino la cadena de suplencias no aplica.
						var request = new DesignarDocenteRequest(CatedraID: _catedraStore.Catedra,
																 DocenteID: SituacionRevistaINSERT.DocenteID,
																 Cargo: SituacionRevistaINSERT.Cargo,
																 FechaInicio: SituacionRevistaINSERT.FechaInicio,
																 FechaFin: SituacionRevistaINSERT.FechaFin,
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
				_gestionMateriasNavigationService.Navigate();
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
