using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCatedras.DTOs.Requests;
using Core.ServicioCatedras;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes;
using Core.Shared.DTOs.Personas.Requests;
using Microsoft.IdentityModel.Tokens;
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

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;

internal partial class GestionSituacionRevistaViewModel : ObservableValidator
{
	#region Services
	private readonly INavigationService _gestionMateriasNavigationService;
	private readonly IServicioDocente _servicioDocente;
	private readonly IServicioCatedra _servicioCatedra;
	private readonly IDialogService _dialogService;
	#endregion

	#region Store
	private readonly CursoStore _cursoStore = null;

	private readonly MateriaStore _materiaStore = null;

	private readonly CatedraStore _catedraStore = null;
	#endregion

	#region ViewModels
	[ObservableProperty]
	private MateriaViewModel _materia;

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

	[NotifyCanExecuteChangedFor(nameof(ListarCommand))]
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

	[Required(AllowEmptyStrings=false, ErrorMessage="Debe ingresar el docente que desea buscar")]
	[RegularExpression(@"^[A-Za-zÀ-ÿ]+( [A-Za-zÀ-ÿ]+)*$", ErrorMessage="Solo debe ingresar nombre, apellido o una combinación de ambos.")]
	[NotifyDataErrorInfo]
	[NotifyCanExecuteChangedFor(nameof(ListarCommand))]
	[ObservableProperty]
	private string _query = string.Empty;

	[ObservableProperty]
	private ObservableCollection<LegajoDocenteViewModel> _docentes = new();

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
	public IAsyncRelayCommand ListarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	public IRelayCommand RegistrarCommand { get; }
	public IRelayCommand SeleccionarCommand { get; }
	#endregion


	public GestionSituacionRevistaViewModel(INavigationService gestionMateriasNavigationService,
											IServicioDocente servicioDocente,
											IServicioCatedra servicioCatedra,
											IDialogService dialogService,
											CursoStore cursoStore,
											MateriaStore materiaStore,
											CatedraStore catedraStore)
	{
		_gestionMateriasNavigationService = gestionMateriasNavigationService;
		_servicioDocente = servicioDocente;
		_servicioCatedra = servicioCatedra;
		_dialogService = dialogService;

		_cursoStore = cursoStore;
		_materiaStore = materiaStore;
		_catedraStore = catedraStore;

		CargarSituacionRevistasCommandAsync = new AsyncRelayCommand(CargarSituacionRevistasAsync);
		CancelarCommand = new AsyncRelayCommand<string>(ExecuteCancelarCommandAsync, CanExecuteCancelarCommand);
		EditarCommand = new AsyncRelayCommand<string>(ExecuteEditarCommandAsync, CanExecuteEditarCommand);
		EliminarCommand = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		GuardarCommand = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);
		ListarCommand = new AsyncRelayCommand<string>(ExecuteListarCommandAsync, CanExecuteListarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);
		RegistrarCommand = new RelayCommand<string>(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
		SeleccionarCommand = new RelayCommand(ExecuteSeleccionarCommand, CanExecuteCommand);

		// Va después de los comandos: el setter generado notifica a los comandos que declaran [NotifyCanExecuteChangedFor].
		Materia = _materiaStore.Materia;
	}

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
		!situacion.Estado.Equals("Finalizado") && (situacion.FechaFin is null || situacion.FechaFin.Value.Date >= DateTime.Today);

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
				Query = string.Empty;
				HabilitarNuevaSituacionRevista = false;
				HabilitarResultadoBuscar = false;
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
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		/*
		case "Docente":
			messageBoxText = $"¿Está seguro que desea cambiar de situación de revista al docente {SituacionRevistaUPDATE.Docente} " +
							 $"de la materia {_materiaStore.Materia.Descripcion}? Se va a quitar de funciones al docente del cargo {SituacionRevistaUPDATE.Cargo} " +
							 $"(Fecha Alta: {SituacionRevistaUPDATE.FechaAlta.ToString("D")})";
			caption = "Cambio de Situación de Revista";
			result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
			if (result is MessageBoxResult.Yes)
			{
				try
				{
					var request = new EliminarSituacionRevistaRequest(CursoID: _cursoStore.Curso.CursoID,
																	  MateriaID: _materiaStore.Materia.MateriaID,
																	  DocenteID: SituacionRevistaUPDATE.DocenteID);
					await _servicioCurricula.RescindirCargoDocenteDeMateriaAsync(request);
					CargarSituacionRevistas();

					messageBoxText = $"Se quitó del cargo al docente correctamente.";
					caption = "Operación Exitosa";
					MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			SituacionRevistaUPDATE = null;
			break;

			"Rescindir":
				break;
		}*/

		switch (obj)
		{
			case "Docente":
				messageBoxText = $"¿Está seguro que desea quitar de funciones al docente { SituacionRevistaUPDATE.Docente }? El cargo docente de { SituacionRevistaUPDATE.Cargo.ToLower() } continúa asignado al docente aunque el mismo no se encuentre en funciónes.";
				caption = "Quitar docente de funciones";

				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						var request = new RelevarDeFuncionesRequest(CatedraID: _catedraStore.Catedra);
						await _servicioCatedra.RelevarDeFuncionesAsync(request);

						messageBoxText = $"Se relevó correctamente al docente de la materia.";
						caption = "Operación Exitosa";
						_dialogService.MostrarInformacion(messageBoxText, caption);
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
					}
				}

				await CargarSituacionRevistasAsync();
				break;

			case "Rescindir":
				messageBoxText = $"¿Está seguro que desea rescindir el cargo docente de { SituacionRevistaUPDATE.Cargo } al docente { SituacionRevistaUPDATE.Docente }? El cargo docente se encontrará disponible en la asignatura.\n" +
								 $"\nFecha Alta: { SituacionRevistaUPDATE.FechaInicio.ToString("D") }" +
								 $"\nFecha Cargo: { DateTime.Today.ToString("D") }";
				caption = "Rescindir Cargo Docente";

				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						var request = new EstablecerFinDeDesignacionRequest(CatedraID: _catedraStore.Catedra,
																			SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID,
																			FechaFin: DateTime.Today);
						await _servicioCatedra.EstablecerFinDeDesignacionAsync(request);
						await CargarSituacionRevistasAsync();

						messageBoxText = $"Docente liberado correctamente del cargo docente en la materia.";
						caption = "Operación Exitosa";
						_dialogService.MostrarInformacion(messageBoxText, caption);
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
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
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		switch (obj)
		{
			case "Docente":
				messageBoxText = $"¿Está seguro que desea finalizar la designación del cargo docente asignado al docente { SituacionRevistaUPDATE.Docente }?\n" +
								 $"La designación queda finalizada y el cargo deja de estar vigente en la materia.\n" +
								 $"Fecha Alta: { SituacionRevistaUPDATE.FechaInicio.ToString("D") }";
				caption = "Finalizar Designación";

				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						var request = new FinalizarDesignacionRequest(CatedraID: _catedraStore.Catedra,
																	  SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID);
						await _servicioCatedra.FinalizarDesignacionAsync(request);
						await CargarSituacionRevistasAsync();

						messageBoxText = $"Se finalizó correctamente la designación del cargo docente.";
						caption = "Operación Exitosa";
						_dialogService.MostrarInformacion(messageBoxText, caption);
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
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
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		switch (obj)
		{
			case "Docente":
				messageBoxText = $"¿Está seguro que desea establecer como docente de aula a { SituacionRevistaUPDATE.Docente } en la asignatura de { _materiaStore.Materia.Descripcion } con el cargo de { SituacionRevistaUPDATE.Cargo }?";
				caption = "Establecer docente de aula";

				if (_dialogService.Confirmar(messageBoxText, caption))
				{
					try
					{
						var request = new PonerEnFuncionesRequest(CatedraID: _catedraStore.Catedra,
																  SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID);
						await _servicioCatedra.PonerEnFuncionesAsync(request);
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
					}
				}

				await CargarSituacionRevistasAsync();
				break;

			case "Insert":
				if (HabilitarReemplazaA && SituacionRevistaINSERT.ReemplazaA is null)
				{
					_dialogService.MostrarAdvertencia("Debe seleccionar a quién reemplaza el docente suplente.", "Cambio de Situación Revista");
					return;
				}

				messageBoxText = $"¿Está seguro que desea realizar un cambio en la situación de revista del docente {SituacionRevistaINSERT.Docente}?\n" +
								 $"Materia: {_materiaStore.Materia.Descripcion}\n" +
								 $"Cargo: {SituacionRevistaINSERT.Cargo}\n\n";
				caption = "Cambio de Situación Revista";

				if (_dialogService.Confirmar(messageBoxText, caption))
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

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";
						_dialogService.MostrarInformacion(messageBoxText, caption);
					}
					catch (Exception ex)
					{
						_dialogService.MostrarError(ex.Message, "Error en la operación");
					}
					finally
					{
						Query = string.Empty;
						HabilitarInsert = false;
						HabilitarNuevaSituacionRevista = false;
					}

					await CargarSituacionRevistasAsync();
				}
				break;
		}
	}
	#endregion

	#region ListarCommand
	private bool CanExecuteListarCommand(object obj) => obj switch
	{
		"Query" => !HasErrors,
		"Listar" => HabilitarInsert,
		_ => false
	};

	private async Task ExecuteListarCommandAsync(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		switch (obj)
		{
			case "Query":
				try
				{
					var request = new NombreCompletoRequest(Query);
					var response = await _servicioDocente.BuscarDocenteSegunNombreCompletoAsync(request);

					if (response.IsNullOrEmpty())
					{
						messageBoxText = $"No existen coincidencias.";
						caption = "Buscar";
						_dialogService.MostrarAdvertencia(messageBoxText, caption);

						HabilitarResultadoBuscar = false;

						return;
					}

					Docentes = new ObservableCollection<LegajoDocenteViewModel>(response.Select(x => new LegajoDocenteViewModel(x)));
					HabilitarResultadoBuscar = true;

					messageBoxText = $"Se encontraron {Docentes.Count} coincidencias.";
					caption = "Operación Exitosa";
					_dialogService.MostrarInformacion(messageBoxText, caption);
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "Error en la operación");
				}

				break;

				/*
				case "Listar":
					try
					{
						var response = await _servicioDocente.ListarDocentesActivosAsync();
						if (response.IsNullOrEmpty())
						{
							messageBoxText = $"No existen docentes activos.";
							caption = "Listado docente";
							result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);

							HabilitarResultadoBuscar = false;
							return;
						}

						Docentes = new ObservableCollection<LegajoDocenteViewModel>(response.Select(x => new LegajoDocenteViewModel(x)));
						HabilitarResultadoBuscar = true;

						messageBoxText = $"Se encontraron { Docentes.Count } docentes activos.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}

					break;
				*/
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
