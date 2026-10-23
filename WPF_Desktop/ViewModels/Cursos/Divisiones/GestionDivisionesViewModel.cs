using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones;
using Core.Shared.DTOs.Personas.Requests;
using System.Collections.ObjectModel;
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
	[ObservableProperty]
	private bool _habilitarRegistrarDocente;
	#endregion

	#region Notificaciones
	[ObservableProperty]
	private string _message = string.Empty;
	[ObservableProperty]
	private bool _habilitarNotificacion;
	#endregion

	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
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

	public IRelayCommand CancelarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	#endregion


	public GestionDivisionesViewModel(IServicioDivision servicioDivisiones,
									  INavigationService gestionCursosNavigationService,
									  INavigationService gestionCursantesNavigationService,
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

	#region CancelarCommand
	private bool CanExecuteCancelarCommand(object obj) =>
		HabilitarRegistrarDocente;

	private void ExecuteCancelarCommand(object obj)
	{
		HabilitarRegistrarDocente = false;
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
				_divisionStore.Division = Division;
				_gestionCursantesNavigationService.Navigate();
				break;
		}
	}
	#endregion

	#region RegistrarCommandAsync
	private bool CanExecuteRegistrarCommand(object obj) => obj switch
	{
		"Division" => _cursoStore is not null,
		"Docente" => !HabilitarRegistrarDocente,
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
				HabilitarRegistrarDocente = true;
				ExecuteBuscarDocenteCommand();
				break;

			case "Division":
				messageBoxText = $"Se va a registrar una nueva división en { _cursoStore.Curso.Grado.ToLower() } año de { _cursoStore.Curso.NivelEducativo.ToLower() }.\n\n" +
								 $"¿Desea continuar?";
				caption = "Registrar División";
				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						await _servicioDivision.AgregarDivisionAsync(new AgregarDivisionRequest(_cursoStore.Curso.CursoID));
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

	private async Task ExecuteGuardarCommandAsync(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		try
		{
			messageBoxText = $"Se va asignar al docente { Docente.NombreCompleto } como preceptor de la división { Division.Descripcion } de { Curso } año ({ NivelEducativo })\n\n¿Desea continuar?";
			caption = "Asignar Preceptor";
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

			Docente = null;
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la operación");
		}
	}
	#endregion
}