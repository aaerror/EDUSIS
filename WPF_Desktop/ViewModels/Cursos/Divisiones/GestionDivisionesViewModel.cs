using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes;
using Core.Shared.DTOs.Personas.Requests;
using Microsoft.IdentityModel.Tokens;
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

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

internal partial class GestionDivisionesViewModel : ObservableValidator
{
	#region Service
	private readonly IServicioDivision _servicioDivision;
	private readonly IServicioDocente _servicioDocente;
	private readonly INavigationService _gestionCursosNavigationService;
	private readonly INavigationService _gestionCursantesNavigationService;
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

	[ObservableProperty]
	private ObservableCollection<LegajoDocenteViewModel> _docentes;

	private ObservableCollection<DivisionViewModel> _divisiones;

	[Required(AllowEmptyStrings=true)]
	[DataType(DataType.Text)]
	[RegularExpression(@"^[A-Za-zÀ-ÿ]+( [A-Za-zÀ-ÿ]+)*$", ErrorMessage="Solo debe ingresar nombre, apellido o una combinación de ambos.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _query = string.Empty;

	[ObservableProperty]
	private int _totalDivisiones;
	[ObservableProperty]
	private string _curso;
	[ObservableProperty]
	private string _nivelEducativo;

	#region Flags
	[ObservableProperty]
	private bool _habilitarDivisiones;

	[ObservableProperty]
	private bool _habilitarListaDocentes;

	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(ListarCommandAsync))]
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
	public IAsyncRelayCommand GuardarCommandAsync { get; }
	public IAsyncRelayCommand ListarCommandAsync { get; }
	public IAsyncRelayCommand RegistrarCommandAsync { get; }

	public IRelayCommand CancelarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	#endregion


	public GestionDivisionesViewModel(IServicioDivision servicioDivisiones,
									  IServicioDocente servicioDocentes,
									  INavigationService gestionCursosNavigationService,
									  INavigationService gestionCursantesNavigationService,
									  CursoStore cursoStore,
									  DivisionStore divisionStore,
									  CicloLectivoStore cicloLectivoStore,
									  IDialogService dialogService)
	{
		_servicioDivision = servicioDivisiones;
		_servicioDocente = servicioDocentes;
		_gestionCursosNavigationService = gestionCursosNavigationService;
		_gestionCursantesNavigationService = gestionCursantesNavigationService;
		_cursoStore = cursoStore;
		_divisionStore = divisionStore;
		_cicloLectivoStore = cicloLectivoStore;
		_dialogService = dialogService;

		Curso = _cursoStore.Curso.Grado.ToString();
		NivelEducativo = _cursoStore.Curso.NivelEducativo.ToString();

		_divisiones = new ObservableCollection<DivisionViewModel>();
		_docentes = new ObservableCollection<LegajoDocenteViewModel>();

		CargarDivisionesCommandAsync = new AsyncRelayCommand(CargarDivisionesAsync);
		EliminarCommandAsync = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		GuardarCommandAsync = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);
		ListarCommandAsync = new AsyncRelayCommand<string>(ExecuteListarCommandAsync, CanExecuteListarCommand);
		RegistrarCommandAsync = new AsyncRelayCommand<string>(ExecuteRegistrarCommandAsync, CanExecuteRegistrarCommand);

		CancelarCommand = new RelayCommand<string>(ExecuteCancelarCommand, CanExecuteCancelarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);


		HabilitarRegistrarDocente = false;
	}

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
		Query = string.Empty;
		Docentes.Clear();
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

	#region ListarCommandAsync
	private bool CanExecuteListarCommand(object obj) => obj switch
	{
		"Buscar" => !HasErrors,
		"Listar" => HabilitarRegistrarDocente,
		_ => false
	};

	private async Task ExecuteListarCommandAsync(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		switch (obj)
		{
			case "Buscar":
				try
				{
					Docentes.Clear();
					var request = new NombreCompletoRequest(Query);
					var response = await _servicioDocente.BuscarDocenteSegunNombreCompletoAsync(request);

					if (response.Count is 0)
					{
						messageBoxText = $"No existen coincidencias.";
						caption = "Buscar";
						_dialogService.MostrarAdvertencia(messageBoxText, caption);
						Docentes.Clear();

						HabilitarListaDocentes = false;

						return;
					}

					Docentes = new ObservableCollection<LegajoDocenteViewModel>(response.Select(x => new LegajoDocenteViewModel(x)));
					HabilitarListaDocentes = true;
					
					messageBoxText = $"Se encontraron { Docentes.Count } coincidencias.";
					caption = "Operación Exitosa";
					_dialogService.MostrarInformacion(messageBoxText, caption);
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "Error en la operación");
				}
				break;

			case "Listar":
				try
				{
					var response = await _servicioDocente.ListarDocentesActivosAsync();
					if (response.IsNullOrEmpty())
					{
						messageBoxText = $"No existen coincidencias.";
						caption = "Buscar";
						_dialogService.MostrarAdvertencia(messageBoxText, caption);
						Docentes.Clear();

						HabilitarListaDocentes = false;

						return;
					}

					Docentes = new ObservableCollection<LegajoDocenteViewModel>(response.Select(x => new LegajoDocenteViewModel(x)));
					HabilitarListaDocentes = true;

					messageBoxText = $"Se encontraron { Docentes.Count } coincidencias.";
					caption = "Operación Exitosa";
					_dialogService.MostrarInformacion(messageBoxText, caption);
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "Error en la operación");
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
				HabilitarRegistrarDocente = true;
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
			Query = string.Empty;
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la operación");
		}
	}
	#endregion
}