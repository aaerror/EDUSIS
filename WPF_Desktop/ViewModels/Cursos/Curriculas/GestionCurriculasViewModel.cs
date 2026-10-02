using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioCurriculas;
using Core.ServicioMaterias;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas;

internal partial class GestionCurriculasViewModel : ObservableValidator
{
	#region Services
	private readonly INavigationService _gestionCursosNavigationService;
	private readonly INavigationService _gestionCatedrasNavigationService;
	private readonly IServicioCurricula _servicioCurricula;
	private readonly IServicioMateria _servicioMateria;
	private readonly IDialogService _dialogService;
	#endregion

	#region Store
	private readonly CursoStore _cursoStore;
	private readonly MateriaStore _materiaStore;
	#endregion

	private LegajoDocenteViewModel _docente;

	public string Curso => $"{ _cursoStore.Curso.Grado } año de { _cursoStore.Curso.NivelEducativo }".ToUpper();

	#region Registrar Materia
	[Required(AllowEmptyStrings=true, ErrorMessage="Se debe especificar el nombre de la materia.")]
	[NotifyDataErrorInfo]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[ObservableProperty]
	private string _descripcion = string.Empty;

	[Required]
	[NotifyDataErrorInfo]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[ObservableProperty]
	private int _cargaHoraria = 1;
	#endregion

	#region Notifications
	[ObservableProperty]
	private string _mensaje = string.Empty;

	[ObservableProperty]
	private bool _habilitarNotificacion;
	#endregion

	[ObservableProperty]
	private bool _habilitarMaterias = false;

	[ObservableProperty]
	private bool _habilitarCurriculas = false;

	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private bool _habilitarRegistrarMateria = false;

	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private bool _habilitarEditarMateria = false;

	#region Curriculas
	[ObservableProperty]
	public ObservableCollection<CurriculaViewModel> _curriculas = new();

	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	[ObservableProperty]
	private CurriculaViewModel _curricula;
	#endregion

	#region Materias
	[ObservableProperty]
	public ObservableCollection<MateriaViewModel> _materias = new();

	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[ObservableProperty]
	private MateriaViewModel _materia;
	#endregion

	#region Commands
	public IAsyncRelayCommand CargarCurriculasCommandAsync { get; }
	public IAsyncRelayCommand CargarMateriasCommandAsync { get; }
	public IAsyncRelayCommand EliminarCommandAsync { get; }
	public IAsyncRelayCommand GuardarCommandAsync { get; }
	public IAsyncRelayCommand CancelarCommand { get; }
	public IAsyncRelayCommand RegistrarCommand { get; }

	public IRelayCommand EditarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	#endregion


	public GestionCurriculasViewModel(INavigationService gestionCursosNavigationService,
									  INavigationService gestionCatedrasNavigationService,
									  IServicioCurricula servicioCurricula,
									  IServicioMateria servicioMateria,
									  IDialogService dialogService,
									  CursoStore cursoStore,
									  MateriaStore materiaStore)
	{
		_gestionCursosNavigationService = gestionCursosNavigationService;
		_gestionCatedrasNavigationService = gestionCatedrasNavigationService;
		_servicioCurricula = servicioCurricula;
		_servicioMateria = servicioMateria;
		_dialogService = dialogService;
		_cursoStore = cursoStore;
		_materiaStore = materiaStore;

		CargarCurriculasCommandAsync = new AsyncRelayCommand(CargarCurriculasAsync);
		CargarMateriasCommandAsync = new AsyncRelayCommand(CargarMateriasAsync);

		CancelarCommand = new AsyncRelayCommand<string>(ExecuteCancelarCommandAsync, CanExecuteCancelarCommand);
		EliminarCommandAsync = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		GuardarCommandAsync = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);

		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);
		EditarCommand = new RelayCommand<string>(ExecuteEditarCommand, CanExecuteEditarCommand);
		RegistrarCommand = new AsyncRelayCommand<string>(ExecuteRegistrarCommandAsync, CanExecuteRegistrarCommand);

		HabilitarNotificacion = false;
		HabilitarEditarMateria = false;
	}

	public async Task CargarCurriculasAsync()
	{
		Materias.Clear();
		Curriculas.Clear();
		try
		{
			var request = new CursoRequest(_cursoStore.Curso.CursoID);
			var response = await _servicioCurricula.ListarCurriculasSegunCursoAsync(request);
			if (response.Count is 0)
			{
				HabilitarCurriculas = false;
				HabilitarNotificacion = true;
				Mensaje = "No existen ningún diseño curricular registrado para este curso.";

				return;
			}

			Curriculas = new ObservableCollection<CurriculaViewModel>(response.Select(x => new CurriculaViewModel(x)));

			HabilitarCurriculas = true;
			HabilitarMaterias = true;
			HabilitarNotificacion = false;
		}
		catch (Exception ex)
		{
			string messageBoxText = $"Error al cargar los diseño curriculares del curso.\nError: { ex.Message }";
			_dialogService.MostrarAdvertencia(messageBoxText, "Error en la operación");

			HabilitarCurriculas = false;
			HabilitarNotificacion = true;
			Mensaje = messageBoxText;
		}
	}

	public async Task CargarMateriasAsync()
	{
		Materias.Clear();
		if (Curriculas.Count is not 0)
		{
			try
			{
				var request = new ListarMateriasSegunCurriculaRequest(CursoID: _cursoStore.Curso.CursoID,
																	  CurriculaID: Curricula.CurriculaID);
				var response = await _servicioMateria.ListarMateriasSegunCurriculaAsync(request);
				if (response.Count is not 0)
				{
					Materias = new ObservableCollection<MateriaViewModel>(response.Select(x => new MateriaViewModel(x)));

					HabilitarMaterias = true;
					HabilitarNotificacion = false;

					HabilitarRegistrarMateria = false;
				}
			}
			catch (Exception ex)
			{
				string messageBoxText = $"Error al cargar las materias del curso.\nError: { ex.Message }";
				_dialogService.MostrarAdvertencia(messageBoxText, "Error en la operación");

				HabilitarMaterias = false;
				HabilitarNotificacion = true;
				Mensaje = messageBoxText;
			}
		}
	}

	#region Commands
	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj) => obj switch
	{
		"Cursos" => !HabilitarRegistrarMateria && !HabilitarEditarMateria,
		"Catedras" => Materia is not null && !HabilitarEditarMateria,
		_ => false
	};

	private void ExecuteNavigationCommand(object obj)
	{
		switch (obj)
		{
			case "Cursos":
				_gestionCursosNavigationService.Navigate();
				break;

			case "Catedras":
				_materiaStore.Materia = Materia;
				_gestionCatedrasNavigationService.Navigate();
				break;
		}
	}
	#endregion

	#region GuardarCommandAsync
	private bool CanExecuteGuardarCommand(object obj) => obj switch
	{
		"Materia" => !HasErrors,
		"Update" => HabilitarEditarMateria,
		_ => false
	};

	private async Task ExecuteGuardarCommandAsync(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		switch (obj)
		{
			case "Materia":
				if (string.IsNullOrWhiteSpace(Descripcion))
				{
					messageBoxText = $"Error al registrar una materia en el curso. Existen algunos campos vacíos y debe completarlos antes de poder continuar.";
					caption = "Error al Registrar Materia";
					_dialogService.MostrarAdvertencia(messageBoxText, caption);

					break;
				}

				messageBoxText = $"¿Está seguro que desea agregar la materia a la currícula de { _cursoStore.Curso.Grado }° año de la { _cursoStore.Curso.NivelEducativo }?\n\n" +
								 $"Materia: { Descripcion }\nCarga horaria: { CargaHoraria } horas cátedra";
				caption = "Registrar Materia";

				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						var request = new RegistrarMateriaRequest(CursoID: Curricula.CursoID,
																  CurriculaID: Curricula.CurriculaID,
																  Descripcion: Descripcion,
																  HorasCatedra: CargaHoraria);
						await _servicioMateria.RegistrarMateriaAsync(request);

						messageBoxText = $"Nueva materia agregada a la currícula del curso.";
						caption = "Operación Exitosa";

						_dialogService.MostrarInformacion(messageBoxText, caption);

						HabilitarRegistrarMateria = false;

						await CargarCurriculasAsync();
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
						HabilitarRegistrarMateria = false;
					}
					finally
					{
						Descripcion = string.Empty;
						CargaHoraria = 1;
					}
				}

				break;
			case "Update":
				messageBoxText = $"¿Está seguro que desea guardar los cambios en la materia?";
				caption = "Guardar Materia";

				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						var request = new ModificarMateriaRequest(CursoID: Curricula.CursoID,
																  CurriculaID: Curricula.CurriculaID,
																  MateriaID: Materia.MateriaID,
																  Descripcion: Materia.Descripcion,
																  HorasCatedra: Materia.HorasCatedra);
						await _servicioMateria.ModificarMateriaAsync(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";

						_dialogService.MostrarInformacion(messageBoxText, caption);

						HabilitarEditarMateria = false;
						await CargarMateriasAsync();
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");

						HabilitarEditarMateria = false;
					}
				}
				break;
		}
	}
	#endregion

	#region EditarCommand
	private bool CanExecuteEditarCommand(object obj) => obj switch
	{
		"Materia" => Materia is not null && !HabilitarEditarMateria,
		_ => false
	};

	private void ExecuteEditarCommand(object obj)
	{
		switch (obj)
		{
			case "Materia":
				HabilitarRegistrarMateria = false;
				HabilitarEditarMateria = true;

				break;
		}
	}
	#endregion

	#region RegistrarCommand
	private bool CanExecuteRegistrarCommand(object obj) => obj switch
	{
		"Curricula" => !HabilitarRegistrarMateria && !HabilitarEditarMateria,
		"Materia" => Curricula is not null && !HabilitarRegistrarMateria && !HabilitarEditarMateria,
		_ => false
	};

	private async Task ExecuteRegistrarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Curricula":
				string messageBoxText = string.Empty;
				string caption = string.Empty;

				messageBoxText = $"¿Está seguro que desea crear un nuevo diseño curricular? Se procedera a crear un diseño curricular con fecha inicio desde la fecha de hoy, { DateTime.Today.ToString("D") }. El diseño curricular que se encuentra vigente dejara de estarlo en el día de la fecha.";
				caption = "Nuevo diseño curricular";
				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						var request = new RegistrarCurriculaRequest(CursoID: _cursoStore.Curso.CursoID,
																	FechaInicio: DateTime.Today,
																	FechaFin: null);
						await _servicioCurricula.RegistrarCurriculaAsync(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";

						_dialogService.MostrarInformacion(messageBoxText, caption);

						HabilitarEditarMateria = false;
						await CargarMateriasAsync();
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
					}
				}

				break;

			case "Update":
				HabilitarRegistrarMateria = false;
				HabilitarEditarMateria = true;

				break;

			case "Materia":
				HabilitarNotificacion = false;
				HabilitarCurriculas = false;

				HabilitarRegistrarMateria = true;
				break;
		}
	}
	#endregion

	#region EliminarCommandAsync
	private bool CanExecuteEliminarCommand(object obj) => obj switch
	{
		"Materia" => Materia is not null && !HabilitarEditarMateria,
		_ => false
	};

	private async Task ExecuteEliminarCommandAsync(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		switch (obj)
		{
			case "Materia":
				messageBoxText = $"¿Está seguro que desea eliminar la materia de {Materia.Descripcion} del diseño curricular del curso?";
				caption = "Eliminar Materia";

				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						var request = new EliminarMateriaRequest(CursoID: Curricula.CursoID,
																 CurriculaID: Curricula.CurriculaID,
																 MateriaID: Materia.MateriaID);
						await _servicioMateria.EliminarMateriaAsync(request);

						messageBoxText = $"La materia se eliminó correctamente de la currícula del curso.";
						caption = "Operación Exitosa";
						_dialogService.MostrarInformacion(messageBoxText, caption);

						await CargarMateriasAsync();
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

	#region CancelarCommand
	private bool CanExecuteCancelarCommand(object obj) => obj switch
	{
		"Nueva" => HabilitarRegistrarMateria,
		"Editar" => HabilitarEditarMateria,
		_ => false
	};

	private async Task ExecuteCancelarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Nueva":
				HabilitarRegistrarMateria = false;

				await CargarCurriculasAsync();
				break;

			case "Editar":
				HabilitarEditarMateria = false;
				await CargarMateriasAsync();
				break;

			default:
				break;
		}
	}
	#endregion
	#endregion
}
