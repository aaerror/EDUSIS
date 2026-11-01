using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioCurriculas;
using Core.ServicioMaterias;
using System.Collections.Generic;
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

internal partial class GestionCurriculasViewModel : ObservableValidator, IUbicacion
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

		VolverCommand = new RelayCommand(gestionCursosNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Hacia("CURSOS", VolverCommand),
			Migaja.Actual($"{ Curso } · DISEÑO CURRICULAR")
		};
	}

	/// <summary>
	/// Ruta hasta el diseño curricular. El curso vive acá y no en el título: el título dice qué
	/// es la pantalla y antes decía el grado, con lo cual la palabra "diseño curricular" no
	/// aparecía en ninguna parte.
	/// </summary>
	public IReadOnlyList<Migaja> Ruta { get; }

	public IRelayCommand VolverCommand { get; }

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
		if (Curricula is not null)
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
				_materiaStore.Materia = new MateriaSeleccionada(Materia.CursoID, Materia.MateriaID, Materia.Descripcion);
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
					_dialogService.MostrarAdvertencia("Escribí el nombre de la materia antes de agregarla.", "Falta el nombre de la materia");

					break;
				}

				// La carga horaria viaja en el texto y la guarda estática W5 lo comprueba: el
				// número que el usuario lee en el diálogo tiene que ser el que sale en el request.
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Agregar { Descripcion } al diseño curricular",
						mensaje: $"Queda en { Grados.Decir(_cursoStore.Curso.Grado) } de { _cursoStore.Curso.NivelEducativo }. "
							   + $"Carga horaria: { CargaHoraria } horas cátedra.",
						verbo: "Agregar materia")))
				{
					try
					{
						var request = new RegistrarMateriaRequest(CursoID: Curricula.CursoID,
																  CurriculaID: Curricula.CurriculaID,
																  Descripcion: Descripcion,
																  HorasCatedra: CargaHoraria);
						await _servicioMateria.RegistrarMateriaAsync(request);

						_dialogService.MostrarInformacion($"{ Descripcion }, { CargaHoraria } horas cátedra.", "Materia agregada");

						HabilitarRegistrarMateria = false;

						await CargarMateriasAsync();
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
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Guardar los cambios de { Materia.Descripcion }",
						mensaje: $"Carga horaria: { Materia.HorasCatedra?.ToString() ?? "sin definir" } horas cátedra. "
							   + "La carga horaria es la que reparten las cátedras de la materia entre sus docentes.",
						verbo: "Guardar materia")))
				{
					try
					{
						var request = new ModificarMateriaRequest(CursoID: Curricula.CursoID,
																  CurriculaID: Curricula.CurriculaID,
																  MateriaID: Materia.MateriaID,
																  Descripcion: Materia.Descripcion,
																  HorasCatedra: Materia.HorasCatedra.HasValue ? Materia.HorasCatedra.Value : 0);
						await _servicioMateria.ModificarMateriaAsync(request);

						_dialogService.MostrarInformacion($"{ Materia.Descripcion }.", "Materia actualizada");

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
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: "Abrir un diseño curricular nuevo",
						mensaje: $"Empieza hoy, { DateTime.Today.ToString("D") }, y el diseño curricular vigente se cierra con la fecha de hoy. "
							   + "Las materias del diseño cerrado quedan como registro de los años anteriores.",
						verbo: "Crear currícula")))
				{
					try
					{
						var request = new RegistrarCurriculaRequest(CursoID: _cursoStore.Curso.CursoID,
																	FechaInicio: DateTime.Today,
																	FechaFin: null);
						await _servicioCurricula.RegistrarCurriculaAsync(request);

						_dialogService.MostrarInformacion($"Vigente desde hoy, { DateTime.Today.ToString("d") }. Ya podés agregarle materias.", "Currícula creada");

						HabilitarEditarMateria = false;
						await CargarCurriculasAsync();
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
		switch (obj)
		{
			case "Materia":
				if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Quitar { Materia.Descripcion } del diseño curricular",
						consecuencia: "La materia deja de figurar en el diseño curricular del curso y no se le podrán cargar "
									+ "calificaciones. Core rechaza la operación si la materia ya tiene cátedras con docentes designados.",
						verbo: "Quitar materia")))
				{
					try
					{
						var request = new EliminarMateriaRequest(CursoID: Curricula.CursoID,
																 CurriculaID: Curricula.CurriculaID,
																 MateriaID: Materia.MateriaID);
						await _servicioMateria.EliminarMateriaAsync(request);

						_dialogService.MostrarInformacion($"{ Materia.Descripcion }.", "Materia quitada");

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

				await CargarMateriasAsync();
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
