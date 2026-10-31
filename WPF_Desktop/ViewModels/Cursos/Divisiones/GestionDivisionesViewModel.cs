using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones;
using Core.Shared.DTOs.Personas.Requests;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Docentes;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

internal partial class GestionDivisionesViewModel : ObservableValidator
{
	#region Service
	private readonly IServicioDivision _servicioDivision;
	private readonly INavigationService _gestionCursosNavigationService;
	private readonly INavigationService _gestionCursantesNavigationService;
	private readonly INavigationService _gestionAsistenciasNavigationService;
	private readonly INavigationService _buscarDocenteModalNavigationService;
	#endregion

	#region Mensajeria
	private readonly IMessenger _messenger;
	#endregion

	#region Stores
	private readonly CursoStore _cursoStore;
	private readonly DivisionStore _divisionStore;
	private readonly CicloLectivoStore _cicloLectivoStore;
	#endregion

	#region Dialogos
	private readonly IDialogService _dialogService;
	#endregion

	#region Request
	private EliminarDivisionRequest request;
	#endregion

	#region ViewModels
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[ObservableProperty]
	private LegajoDocenteViewModel _docente;
	#endregion

	private ObservableCollection<DivisionViewModel> _divisiones;

	#region Formulario
	/// <summary>
	/// La letra de la división nueva. El sistema la propone al cargar la pantalla
	/// (<c>ProponerDescripcionDivisionAsync</c>) y el usuario la puede cambiar: es la decisión de
	/// DIV-1. La validación de acá es de forma —una sola letra— y no reemplaza a la del dominio:
	/// que esa letra no esté ya registrada en el curso lo decide <c>AgregarDivisionAsync</c> con
	/// <c>DivisionDuplicadaException</c>, porque es una regla que depende del estado de la base.
	/// </summary>
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommandAsync))]
	[NotifyDataErrorInfo]
	[Required(ErrorMessage = "Indique la letra de la división.")]
	[RegularExpression(@"^[A-Za-z]$", ErrorMessage = "La división se identifica con una sola letra.")]
	[ObservableProperty]
	private string _descripcion = string.Empty;
	#endregion

	[ObservableProperty]
	private int _totalDivisiones;
	[ObservableProperty]
	private string _curso;
	[ObservableProperty]
	private string _nivelEducativo;

	#region Flags
	[ObservableProperty]
	private bool _habilitarDivisiones;

	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(ModificarPreceptorCommand))]
	[ObservableProperty]
	private bool _habilitarRegistrarDocente;

	/// <summary>
	/// Distingue si la tarjeta de asignación se abrió para asignar un preceptor a un cargo vacante
	/// o para reemplazar al que ya está. Hace falta porque el buscador vive en el modal y devuelve
	/// el docente por <c>DocenteElegidoMessage</c> sin saber quién lo abrió: la intención se
	/// registra acá antes de navegar y <c>GuardarCommandAsync</c> la lee para elegir el caso de uso.
	/// </summary>
	[ObservableProperty]
	private bool _modificandoPreceptor;
	#endregion

	#region Notificaciones
	[ObservableProperty]
	private string _message = string.Empty;
	[ObservableProperty]
	private bool _habilitarNotificacion;
	#endregion

	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(ModificarPreceptorCommand))]
	[ObservableProperty]
	private DivisionViewModel _division;
	[ObservableProperty]
	private ListCollectionView _listCollectionDocentes;

	#region Commands
	public IAsyncRelayCommand CargarDivisionesCommandAsync { get; }
	public IAsyncRelayCommand EliminarCommandAsync { get; }
	public IRelayCommand BuscarDocenteCommand { get; }
	public IAsyncRelayCommand GuardarCommandAsync { get; }
	public IAsyncRelayCommand RegistrarCommandAsync { get; }
	public IRelayCommand ModificarPreceptorCommand { get; }

	public IRelayCommand CancelarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	#endregion


	/// <remarks>
	/// ORDEN DE LOS CUATRO <see cref="INavigationService"/>: los cuatro son del mismo tipo y la factory
	/// lambda de <c>WPF_DesktopDI</c> los distingue SÓLO por posición. Invertirlos compila y rompe la
	/// navegación en silencio. Orden obligatorio: 1) <paramref name="gestionCursosNavigationService"/>
	/// (volver a cursos), 2) <paramref name="gestionCursantesNavigationService"/> (ir a los alumnos de
	/// la división), 3) <paramref name="gestionAsistenciasNavigationService"/> (ir a la asistencia de
	/// la división), 4) <paramref name="buscarDocenteModalNavigationService"/> (abrir el buscador de
	/// docentes en el modal). Los tres primeros son los destinos del drill-down y van agrupados; el del
	/// modal queda último porque no es una pantalla de este recorrido.
	/// </remarks>
	public GestionDivisionesViewModel(IServicioDivision servicioDivisiones,
									  INavigationService gestionCursosNavigationService,
									  INavigationService gestionCursantesNavigationService,
									  INavigationService gestionAsistenciasNavigationService,
									  INavigationService buscarDocenteModalNavigationService,
									  CursoStore cursoStore,
									  DivisionStore divisionStore,
									  CicloLectivoStore cicloLectivoStore,
									  IDialogService dialogService,
									  IMessenger messenger)
	{
		_servicioDivision = servicioDivisiones;
		_gestionCursosNavigationService = gestionCursosNavigationService;
		_gestionCursantesNavigationService = gestionCursantesNavigationService;
		_gestionAsistenciasNavigationService = gestionAsistenciasNavigationService;
		_buscarDocenteModalNavigationService = buscarDocenteModalNavigationService;
		_cursoStore = cursoStore;
		_divisionStore = divisionStore;
		_cicloLectivoStore = cicloLectivoStore;
		_dialogService = dialogService;
		_messenger = messenger;

		Curso = _cursoStore.Curso.Grado.ToString();
		NivelEducativo = _cursoStore.Curso.NivelEducativo.ToString();

		_divisiones = new ObservableCollection<DivisionViewModel>();

		CargarDivisionesCommandAsync = new AsyncRelayCommand(CargarDivisionesAsync);
		EliminarCommandAsync = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		GuardarCommandAsync = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);
		RegistrarCommandAsync = new AsyncRelayCommand<string>(ExecuteRegistrarCommandAsync, CanExecuteRegistrarCommand);

		ModificarPreceptorCommand = new RelayCommand(ExecuteModificarPreceptorCommand, CanExecuteModificarPreceptorCommand);

		BuscarDocenteCommand = new RelayCommand(ExecuteBuscarDocenteCommand, () => HabilitarRegistrarDocente);
		CancelarCommand = new RelayCommand<string>(ExecuteCancelarCommand, CanExecuteCancelarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);

		// El buscador vive en el modal y no conoce a sus llamadores: el elegido vuelve por acá.
		_messenger.Register<GestionDivisionesViewModel, DocenteElegidoMessage>(this, (receptor, mensaje) =>
			receptor.Docente = mensaje.Docente);

		HabilitarRegistrarDocente = false;
	}

	#region BuscarDocenteCommand
	/// <summary>
	/// Abre el buscador de docentes en el modal. Antes esta pantalla traía su propia copia del
	/// buscador —campo, lupa, "Ver docentes" y grilla de resultados— dentro de la tarjeta de
	/// asignación, con su propio <c>Query</c> validado y su propia colección de docentes.
	/// </summary>
	private void ExecuteBuscarDocenteCommand() =>
		_buscarDocenteModalNavigationService.Navigate();
	#endregion

	private async Task CargarDivisionesAsync()
	{
		_divisiones.Clear();
		try
		{
			var divisiones = await _servicioDivision.ListarDivisionesAsync(new ListarDivisionesRequest(_cursoStore.Curso.CursoID, _cicloLectivoStore.CicloLectivo));
			TotalDivisiones = divisiones.Count;

			// La propuesta va antes del corte por lista vacía: el curso sin divisiones es justamente
			// el caso donde más sirve tener la letra precargada ("A"), y después del return no se
			// ejecutaría nada.
			await ProponerDescripcionAsync();

			if (divisiones.Count is 0)
			{
				HabilitarDivisiones = false;
				HabilitarNotificacion = true;

				Message = "No existen divisiones agregadas en el curso hasta el momento. Debe registrar al menos una división en el curso para poder trabajar.";

				return;
			}

			_divisiones = new ObservableCollection<DivisionViewModel>(divisiones.Select(x => new DivisionViewModel(x)));
			ListCollectionDocentes = new ListCollectionView(_divisiones);

			HabilitarDivisiones = true;
			HabilitarNotificacion = false;
		}
		catch (Exception ex)
		{
			HabilitarDivisiones = false;
			HabilitarNotificacion = true;

			Message = $"Error al cargar las materias del curso.\nError: { ex.Message }";
		}
	}

	/// <summary>
	/// Precarga la letra que propone el dominio. Va en su propio <c>try</c> y no en el de
	/// <c>CargarDivisionesAsync</c> a propósito: quedarse sin letras —el curso llegó a la "Z",
	/// <c>SinLetrasDisponiblesException</c>— no tiene que impedir ver ni administrar las divisiones
	/// que ya existen, que es todo lo demás que hace esta pantalla. El usuario se enterará al
	/// intentar registrar, con el mensaje del dominio, que es donde la regla vive.
	/// </summary>
	private async Task ProponerDescripcionAsync()
	{
		try
		{
			Descripcion = await _servicioDivision.ProponerDescripcionDivisionAsync(new ProponerDescripcionRequest(_cursoStore.Curso.CursoID));
		}
		catch (Exception)
		{
			Descripcion = string.Empty;
		}
	}

	#region CancelarCommand
	private bool CanExecuteCancelarCommand(object obj) =>
		HabilitarRegistrarDocente;

	private void ExecuteCancelarCommand(object obj)
	{
		HabilitarRegistrarDocente = false;
		ModificandoPreceptor = false;
		Docente = null;
	}
	#endregion

	#region ModificarPreceptorCommand
	/// <summary>
	/// Abre la misma tarjeta y el mismo buscador que la asignación; lo único que cambia es el caso
	/// de uso que ejecuta <c>GuardarCommandAsync</c> después. No lleva el sufijo <c>Async</c>
	/// porque no hace trabajo asincrónico: deja la intención registrada y navega al modal. El
	/// trabajo contra <c>Core</c> ocurre al guardar, cuando ya hay un docente elegido.
	/// </summary>
	private bool CanExecuteModificarPreceptorCommand() =>
		!HabilitarRegistrarDocente && Division is not null && Division.PreceptorID is not null;

	private void ExecuteModificarPreceptorCommand()
	{
		ModificandoPreceptor = true;
		HabilitarRegistrarDocente = true;
		ExecuteBuscarDocenteCommand();
	}
	#endregion

	#region EliminarCommandAsync
	private bool CanExecuteEliminarCommand(object obj) => obj switch
	{
		"Division" => Division is not null,
		"Preceptor" => !HabilitarRegistrarDocente && Division is not null && Division.PreceptorID is not null,
		_ => false
	};

	private async Task ExecuteEliminarCommandAsync(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		switch (obj)
		{
			case "Division":
				messageBoxText = $"Se va a eliminar la división { Division.Descripcion } del curso.\n\n" +
								 $"¿Desea continuar?";
				caption = "Eliminar División";
				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						var request = new EliminarDivisionRequest(_cursoStore.Curso.CursoID, Division.DivisionID, _cicloLectivoStore.CicloLectivo);
						await _servicioDivision.EliminarDivisionAsync(request);

						_dialogService.MostrarInformacion("División eliminada correctamente", "Operación exitosa");

						await CargarDivisionesAsync();
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
					}
				}

				break;

			case "Preceptor":
				messageBoxText = $"Se va a quitar el preceptor de la división { Division.Descripcion }.\n\n" +
								 $"¿Desea continuar?";
				caption = "Quitar Preceptor";

				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						var request = new EliminarPreceptorRequest(_cursoStore.Curso.CursoID, Division.DivisionID);
						await _servicioDivision.QuitarPreceptorAsync(request);

						_dialogService.MostrarInformacion("Se dio de baja correctamente el preceptor de la división", "Operación exitosa");

						await CargarDivisionesAsync();
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
					}
				}
				break;
		}
	}
	#endregion


	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj) => obj switch
	{
		"Curso" => !HabilitarRegistrarDocente,
		"Cursantes" => Division is not null && !HabilitarRegistrarDocente,
		"Asistencias" => Division is not null && !HabilitarRegistrarDocente,
		_ => false
	};

	private void ExecuteNavigationCommand(object obj)
	{
		switch (obj)
		{
			case "Curso":
				_gestionCursosNavigationService.Navigate();
				break;

			case "Cursantes":
				_divisionStore.Division = new DivisionSeleccionada(Division.DivisionID);
				_gestionCursantesNavigationService.Navigate();
				break;

			case "Asistencias":
				// La división es el parámetro de navegación: se escribe ANTES de navegar porque el
				// ViewModel destino la lee en su constructor, y el destino nace recién en Navigate().
				_divisionStore.Division = new DivisionSeleccionada(Division.DivisionID);
				_gestionAsistenciasNavigationService.Navigate();
				break;
		}
	}
	#endregion

	#region RegistrarCommandAsync
	/// <remarks>
	/// El caso "Docente" exige que el cargo esté <strong>vacante</strong>. Antes pedía sólo
	/// <c>!HabilitarRegistrarDocente</c>, así que "Asignar preceptor" quedaba habilitado con el
	/// cargo ya ocupado y el recorrido terminaba en un <c>CargoPreceptorNoDisponibleException</c>
	/// mostrado como error: el usuario descubría la regla chocándose con ella. Para reemplazar al
	/// preceptor está <c>ModificarPreceptorCommand</c>, que se habilita con la condición inversa.
	/// </remarks>
	private bool CanExecuteRegistrarCommand(object obj) => obj switch
	{
		"Division" => !HabilitarRegistrarDocente && !string.IsNullOrWhiteSpace(Descripcion) && !HasErrors,
		"Docente" => !HabilitarRegistrarDocente && Division is not null && Division.PreceptorID is null,
		_ => false
	};

	private async Task ExecuteRegistrarCommandAsync (object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		switch (obj)
		{
			case "Docente":
				// Abre la tarjeta de asignacion y, encima, el buscador en el modal.
				ModificandoPreceptor = false;
				HabilitarRegistrarDocente = true;
				ExecuteBuscarDocenteCommand();
				break;

			case "Division":
				messageBoxText = $"Se va a registrar la división { Descripcion.ToUpper() } en { _cursoStore.Curso.Grado.ToLower() } año de { _cursoStore.Curso.NivelEducativo.ToLower() }.\n\n" +
								 $"¿Desea continuar?";
				caption = "Registrar División";
				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						await _servicioDivision.AgregarDivisionAsync(new AgregarDivisionRequest(_cursoStore.Curso.CursoID, Descripcion));
						_dialogService.MostrarInformacion("Datos guardados correctamente", "Operación exitosa");
						await CargarDivisionesAsync();
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
					}
				}

				break;
		}
	}
	#endregion

	#region GuardarCommandAsync
	private bool CanExecuteGuardarCommand(object obj) =>
		Docente is not null;

	/// <summary>
	/// Un solo botón "Guardar" cierra los dos recorridos, porque los dos terminan con un docente
	/// elegido en el modal: <c>ModificandoPreceptor</c> decide cuál. Reemplazar al preceptor no es
	/// quitar y volver a asignar —eso dejaría el cargo vacante en el medio— sino
	/// <c>ModificarPreceptorAsync</c>, una sola unidad de trabajo.
	/// </summary>
	private async Task ExecuteGuardarCommandAsync(object obj)
	{
		try
		{
			if (ModificandoPreceptor)
			{
				await ModificarPreceptorAsync();
			}
			else
			{
				await AsignarPreceptorAsync();
			}

			Docente = null;
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la operación");
		}
	}

	private async Task AsignarPreceptorAsync()
	{
		var messageBoxText = $"Se va asignar al docente { Docente.NombreCompleto } como preceptor de la división { Division.Descripcion } de { Curso } año ({ NivelEducativo })\n\n¿Desea continuar?";
		var caption = "Asignar Preceptor";

		if (_dialogService.Confirmar(messageBoxText, caption))
		{
			try
			{
				var request = new RegistrarPreceptorRequest(CursoID: _cursoStore.Curso.CursoID,
															DivisionID: Division.DivisionID,
															DocenteID: Docente.DocenteID);

				await _servicioDivision.AsignarPreceptorAsync(request);

				_dialogService.MostrarInformacion("Datos guardados correctamente", "Operación exitosa");
				await CargarDivisionesAsync();
			}
			catch (Exception ex)
			{
				_dialogService.MostrarError(ex.Message, "Error en la operación");
			}
		}
	}

	private async Task ModificarPreceptorAsync()
	{
		var messageBoxText = $"La división { Division.Descripcion } de { Curso } año ({ NivelEducativo }) pasa a estar a cargo del docente { Docente.NombreCompleto }.\n" +
							 $"El preceptor actual, { Division.Preceptor }, deja el cargo.\n\n" +
							 $"¿Desea continuar?";
		var caption = "Cambiar Preceptor";

		if (_dialogService.Confirmar(messageBoxText, caption))
		{
			try
			{
				var request = new ModificarPreceptorRequest(CursoID: _cursoStore.Curso.CursoID,
															DivisionID: Division.DivisionID,
															DocenteID: Docente.DocenteID);

				await _servicioDivision.ModificarPreceptorAsync(request);

				_dialogService.MostrarInformacion("Se cambió correctamente el preceptor de la división", "Operación exitosa");
				await CargarDivisionesAsync();
			}
			catch (Exception ex)
			{
				_dialogService.MostrarError(ex.Message, "Error en la operación");
			}
		}

		ModificandoPreceptor = false;
		HabilitarRegistrarDocente = false;
	}
	#endregion
}