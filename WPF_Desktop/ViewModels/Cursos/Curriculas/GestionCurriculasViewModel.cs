using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas;
using Core.ServicioDocentes;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas;

internal partial class GestionCurriculasViewModel : ObservableValidator
{
	#region Services
	private readonly INavigationService _gestionCursosNavigationService;
	private readonly INavigationService _gestionSituacionRevistaNavigationService;
	private readonly IServicioCurricula _servicioCurricula;
	private readonly IServicioDocente _servicioDocente;
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
	[ObservableProperty]
	private string _descripcion = string.Empty;

	[Required]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private int _cargaHoraria = 0;
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
	[ObservableProperty]
	private MateriaViewModel _materia;
	#endregion

	#region Commands
	public IAsyncRelayCommand EliminarCommandAsync { get; }
	public IAsyncRelayCommand GuardarCommandAsync { get; }

	public IRelayCommand CancelarCommand { get; }
	public IRelayCommand EditarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	public IRelayCommand RegistrarCommand { get; }
	#endregion


	public GestionCurriculasViewModel(INavigationService gestionCursosNavigationService,
									  INavigationService gestionSituacionRevistaNavigationService,
									  IServicioCurricula servicioMateria,
									  IServicioDocente servicioDocente,
									  CursoStore cursoStore,
									  MateriaStore materiaStore)
	{
		_gestionCursosNavigationService = gestionCursosNavigationService;
		_gestionSituacionRevistaNavigationService = gestionSituacionRevistaNavigationService;
		_servicioCurricula = servicioMateria;
		_servicioDocente = servicioDocente;
		_cursoStore = cursoStore;
		_materiaStore = materiaStore;

		CancelarCommand = new RelayCommand<string>(ExecuteCancelarCommand, CanExecuteCancelarCommand);
		EliminarCommandAsync = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		GuardarCommandAsync = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);

		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);
		EditarCommand = new RelayCommand<string>(ExecuteEditarCommand, CanExecuteEditarCommand);
		RegistrarCommand = new RelayCommand<string>(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);

		HabilitarNotificacion = false;
		HabilitarEditarMateria = false;
	}

	public async void CargarCurriculas()
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
			MessageBox.Show(messageBoxText, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Warning);

			HabilitarCurriculas = false;
			HabilitarNotificacion = true;
			Mensaje = messageBoxText;
		}
	}

	public async void CargarMaterias()
	{
		Materias.Clear();
		if (_curriculas.Count is not 0)
		{
			try
			{
				var request = new ListarMateriasSegunCurriculaRequest(CursoID: _cursoStore.Curso.CursoID,
																	  CurriculaID: Curricula.CurriculaID);
				var response = await _servicioCurricula.ListarMateriasSegunCurriculaAsync(request);
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
				MessageBox.Show(messageBoxText, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Warning);

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
		"SituacionRevista" => Materia is not null && !HabilitarEditarMateria,
		_ => false
	};

	private void ExecuteNavigationCommand(object obj)
	{
		switch (obj)
		{
			case "Cursos":
				_gestionCursosNavigationService.Navigate();
				break;

			case "SituacionRevista":
				_materiaStore.Materia = Materia;
				_gestionSituacionRevistaNavigationService.Navigate();
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
		MessageBoxResult result;

		switch (obj)
		{
			case "Materia":
				messageBoxText = $"¿Está seguro que desea registrar la materia?\nMateria: { Descripcion }\nCarga horaria: { CargaHoraria + 1 } hora cátedra";
				caption = "Guardar Materia";
				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);

				if (result is MessageBoxResult.Yes)
				{
					try
					{
						//TODO: Verificar id de curricula
						var request = new RegistrarMateriaRequest(CursoID: Curricula.CursoID,
																  CurriculaID: Curricula.CurriculaID,
																  Descripcion: Descripcion,
																  HorasCatedra: CargaHoraria);
						await _servicioCurricula.RegistrarMateria(request);

						messageBoxText = $"Materia registrada exitósamente.";
						caption = "Operación Exitosa";

						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

						HabilitarRegistrarMateria = false;
						_descripcion = string.Empty;
						_cargaHoraria = 0;

						CargarCurriculas();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
						HabilitarRegistrarMateria = false;
					}
				}

				break;
			case "Update":
				messageBoxText = $"¿Está seguro que desea guardar los cambios en la materia?";
				caption = "Guardar Materia";
				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);

				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new ModificarMateriaRequest(CursoID: Curricula.CursoID,
																  CurriculaID: Curricula.CurriculaID,
																  MateriaID: Materia.MateriaID,
																  Descripcion: Materia.Descripcion,
																  HorasCatedra: Materia.HorasCatedra);
						await _servicioCurricula.ModificarMateriaAsync(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";

						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

						HabilitarEditarMateria = false;
						CargarMaterias();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);

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

	private async void ExecuteRegistrarCommand(object obj)
	{
		switch (obj)
		{
			case "Curricula":
				string messageBoxText = string.Empty;
				string caption = string.Empty;
				MessageBoxResult result;

				messageBoxText = $"¿Está seguro que desea crear un nuevo diseño curricular? Se procedera a crear un diseño curricular con fecha inicio desde la fecha de hoy, { DateTime.Today.ToString("D") }. El diseño curricular que se encuentra vigente dejara de estarlo en el día de la fecha.";
				caption = "Nuevo diseño curricular";
				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						//TODO: Verificar id de curricula
						var request = new RegistrarCurriculaRequest(CursoID: _cursoStore.Curso.CursoID,
																	FechaInicio: DateTime.Today,
																	FechaFin: null);
						await _servicioCurricula.RegistrarCurriculaAsync(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";

						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

						HabilitarEditarMateria = false;
						CargarMaterias();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}

				break;

			case "Update":
				HabilitarRegistrarMateria = false;
				HabilitarEditarMateria = true;
/*
				if (string.IsNullOrWhiteSpace(Descripcion))
				{
					_errorsByProperty.Add(nameof(Descripcion), new List<string>()
					{
						"Se debe especificar el nombre de la materia."
					});
					ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Descripcion)));

					messageBoxText = $"Error al registrar una materia en el curso. Existen algunos campos vacíos y debe completarlos antes de poder continuar.";
					caption = "Error al Registrar Materia";
					MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

					break;
				}

				messageBoxText = $"¿Está seguro que desea agregar la materia a la currícula de { _cursoStore.Curso.Grado }° año de la { _cursoStore.Curso.NivelEducativoDescripcion }?\n\n" +
								 $"Materia: { Descripcion }\nCarga horaria: { CargaHoraria + 1 } horas cátedra";
				caption = "Registrar Materia";
				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);

				if (result is MessageBoxResult.Yes)
				{
					try
					{
						_registrarMateriaRequest = new RegistrarMateriaRequest(CursoID: _cursoStore.Curso.CursoID,
																			   Descripcion: Descripcion,
																			   HorasCatedra: CargaHoraria + 1);
						await _servicioMateria.RegistrarMateria(_registrarMateriaRequest);

						messageBoxText = $"Nueva materia agregada a la currícula del curso.";
						caption = "Operación Exitosa";

						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
						LoadMaterias();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
						var materiaduplicada = ex.GetType();
					}
					finally
					{
						Descripcion = string.Empty;
						CargaHoraria = 0;
					}
				}
*/
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
		MessageBoxResult result;

		switch (obj)
		{
			case "Materia":
				messageBoxText = $"¿Está seguro que desea eliminar la materia de {Materia.Descripcion} del diseño curricular del curso?";
				caption = "Eliminar Materia";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new EliminarMateriaRequest(CursoID: Curricula.CursoID,
																 CurriculaID: Curricula.CurriculaID,
																 MateriaID: Materia.MateriaID);
						await _servicioCurricula.EliminarMateriaAsync(request);

						messageBoxText = $"La materia se eliminó correctamente de la currícula del curso.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

						CargarMaterias();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
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

	private async void ExecuteCancelarCommand(object obj)
	{
		switch (obj)
		{
			case "Nueva":
				HabilitarRegistrarMateria = false;

				CargarCurriculas();
				break;

			case "Editar":
				HabilitarEditarMateria = false;
				CargarMaterias();
				break;

			default:
				break;
		}
	}
	#endregion
	#endregion
}