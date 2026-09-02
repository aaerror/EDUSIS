using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes;
using Core.Shared.DTOs.Personas.Requests;
using Microsoft.IdentityModel.Tokens;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;

internal partial class GestionSituacionRevistaViewModel : ObservableValidator
{
	#region Services
	private readonly INavigationService _gestionMateriasNavigationService;
	private readonly IServicioDocente _servicioDocente;
	private readonly IServicioCurricula _servicioCurricula;
	#endregion

	#region Store
	private readonly CursoStore _cursoStore = null;

	private readonly MateriaStore _materiaStore = null;
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

	[ObservableProperty]
	private bool _habilitarNuevaSituacionRevista;
	
	[ObservableProperty]
	private bool _habilitarResultadoBuscar;
	
	[ObservableProperty]
	private bool _habilitarGestionSituacionRevista;
	
	[ObservableProperty]
	private bool _habilitarInfoSituacionRevista;

	[NotifyCanExecuteChangedFor(nameof(ListarCommand))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	[ObservableProperty]
	private bool _habilitarInsert;

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

	#region Commands
	public IRelayCommand CancelarCommand { get; }
	public IRelayCommand EditarCommand { get; }
	public IRelayCommand EliminarCommand { get; }
	public IRelayCommand GuardarCommand { get; }
	public IRelayCommand ListarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	public IRelayCommand RegistrarCommand { get; }
	public IRelayCommand SeleccionarCommand { get; }
	#endregion


	public GestionSituacionRevistaViewModel(INavigationService gestionMateriasNavigationService,
											IServicioDocente servicioDocente,
											IServicioCurricula servicioCurricula,
											CursoStore cursoStore,
											MateriaStore materiaStore)
	{
		_gestionMateriasNavigationService = gestionMateriasNavigationService;
		_servicioDocente = servicioDocente;
		_servicioCurricula = servicioCurricula;

		_cursoStore = cursoStore;
		_materiaStore = materiaStore;

		Materia = _materiaStore.Materia;

		CancelarCommand = new RelayCommand<string>(ExecuteCancelarCommand, CanExecuteCancelarCommand);
		EditarCommand = new RelayCommand<string>(ExecuteEditarCommand, CanExecuteEditarCommand);
		EliminarCommand = new RelayCommand<string>(ExecuteEliminarCommand, CanExecuteEliminarCommand);
		GuardarCommand = new RelayCommand<string>(ExecuteGuardarCommand, CanExecuteGuardarCommand);
		ListarCommand = new RelayCommand<string>(ExecuteListarCommand, CanExecuteListarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);
		RegistrarCommand = new RelayCommand<string>(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
		SeleccionarCommand = new RelayCommand(ExecuteSeleccionarCommand, CanExecuteCommand);
	}

	public async void CargarSituacionRevistas()
	{
		try
		{
			DocentesEnMateria.Clear();

			var request = new ListarCargosDocenteSegunMateriaRequest(CursoID: _materiaStore.Materia.CursoID,
																	 CurriculaID: _materiaStore.Materia.CurriculaID,
																	 MateriaID: _materiaStore.Materia.MateriaID);
			var docentes = await _servicioCurricula.ListarCargosDocenteSegunMateriaAsync(request);
			if (docentes.Count is 0)
			{
				string messageBoxText = "No existen docentes registrados para esta materia.";
				string caption = "Docentes";
				MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

				Mensaje = messageBoxText;
				HabilitarNotificacion = true;
				HabilitarGestionSituacionRevista = false;

				return;
			}

			DocentesEnMateria = new ObservableCollection<SituacionRevistaViewModel>(docentes.Select(x => new SituacionRevistaViewModel(x)));

			HabilitarNotificacion = false;
			HabilitarGestionSituacionRevista = true;
			HabilitarInfoSituacionRevista = true;
		}
		catch (Exception ex)
		{
			string messageBoxText = $"Error al cargar los docentes de la materia.\nError: {ex.Message}";
			MessageBox.Show(messageBoxText, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Warning);

			Mensaje = messageBoxText;
			HabilitarNotificacion = true;
		}
	}

	#region Commands
	#region CancelarCommand
	private bool CanExecuteCancelarCommand(object obj) => obj switch
	{
		"Insert" => SituacionRevistaINSERT is not null,
		_ => false
	};

	private void ExecuteCancelarCommand(object obj)
	{
		switch (obj)
		{
			case "Insert":
				CargarSituacionRevistas();
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

	private async void ExecuteEditarCommand(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;
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

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new RelevarDocenteDeAulaRequest(CursoID: _materiaStore.Materia.CursoID,
																	  CurriculaID: _materiaStore.Materia.CurriculaID,
																	  MateriaID: _materiaStore.Materia.MateriaID);
						await _servicioCurricula.RelevarDocenteDeFuncionesEnMateriaAsync(request);

						messageBoxText = $"Se relevó correctamente al docente de la materia.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}

				CargarSituacionRevistas();
				break;

			case "Rescindir":
				messageBoxText = $"¿Está seguro que desea rescindir el cargo docente de { SituacionRevistaUPDATE.Cargo } al docente { SituacionRevistaUPDATE.Docente }? El cargo docente se encontrará disponible en la asignatura.\n" +
								 $"\nFecha Alta: { SituacionRevistaUPDATE.FechaAlta.ToString("D") }" +
								 $"\nFecha Cargo: { DateTime.Today.ToString("D") }";
				caption = "Rescindir Cargo Docente";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new RescindirCargoDocenteRequest(CursoID: _materiaStore.Materia.CursoID,
																	   CurriculaID: _materiaStore.Materia.CurriculaID,
																	   MateriaID: SituacionRevistaUPDATE.MateriaID,
																	   SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID);
						await _servicioCurricula.RescindirCargoDocenteDeMateriaAsync(request);
						CargarSituacionRevistas();

						messageBoxText = $"Docente liberado correctamente del cargo docente en la materia.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}

				CargarSituacionRevistas();
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

	private async void ExecuteEliminarCommand(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		switch (obj)
		{
			case "Docente":
				messageBoxText = $"¿Está seguro que desea eliminar definitivamente el cargo docente asignado al docente { SituacionRevistaUPDATE.Docente }?\n" +
								 $"Esta operación no se puede deshacer.\n" +
								 $"Fecha Alta: { SituacionRevistaUPDATE.FechaAlta.ToString("D") }";
				caption = "Eliminar Cargo Docente";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new EliminarCargoDocenteRequest(CursoID: _materiaStore.Materia.CursoID,
																	  CurriculaID: _materiaStore.Materia.CurriculaID,
																	  MateriaID: SituacionRevistaUPDATE.MateriaID,
																	  SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID);
						await _servicioCurricula.EliminarCargoDocenteAsync(request);
						CargarSituacionRevistas();

						messageBoxText = $"Se eliminó correctamente el cargo docente.";
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

	private async void ExecuteGuardarCommand(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		switch (obj)
		{
			case "Docente":
				messageBoxText = $"¿Está seguro que desea establecer como docente de aula a { SituacionRevistaUPDATE.Docente } en la asignatura de { _materiaStore.Materia.Descripcion } con el cargo de { SituacionRevistaUPDATE.Cargo }?";
				caption = "Establecer docente de aula";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new EstablecerDocenteDeAulaRequest(CursoID: _materiaStore.Materia.CursoID,
																		 CurriculaID: _materiaStore.Materia.CurriculaID,
																		 MateriaID: SituacionRevistaUPDATE.MateriaID,
																		 SituacionRevistaID: SituacionRevistaUPDATE.SituacionRevistaID);
						await _servicioCurricula.EstablecerDocenteDeAulaAsync(request);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}

				CargarSituacionRevistas();
				break;

			case "Insert":
				messageBoxText = $"¿Está seguro que desea realizar un cambio en la situación de revista del docente {SituacionRevistaINSERT.Docente}?\n" +
								 $"Materia: {_materiaStore.Materia.Descripcion}\n" +
								 $"Cargo: {SituacionRevistaINSERT.Cargo}\n\n";
				caption = "Cambio de Situación Revista";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new RegistrarDocenteEnMateriaRequest(CursoID: _materiaStore.Materia.CursoID,
																		   CurriculaID: _materiaStore.Materia.CurriculaID,
																		   MateriaID: _materiaStore.Materia.MateriaID,
																		   DocenteID: SituacionRevistaINSERT.DocenteID,
																		   Cargo: SituacionRevistaINSERT.Cargo,
																		   FechaAlta: SituacionRevistaINSERT.FechaAlta,
																		   FechaBaja: SituacionRevistaINSERT.FechaBaja,
																		   EnFunciones: SituacionRevistaINSERT.EnFunciones);
						await _servicioCurricula.RegistrarDocenteEnMateriaAsync(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
					finally
					{
						Query = string.Empty;
						HabilitarInsert = false;
						HabilitarNuevaSituacionRevista = false;
					}

					CargarSituacionRevistas();
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

	private async void ExecuteListarCommand(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

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
						result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);

						HabilitarResultadoBuscar = false;

						return;
					}

					Docentes = new ObservableCollection<LegajoDocenteViewModel>(response.Select(x => new LegajoDocenteViewModel(x)));
					HabilitarResultadoBuscar = true;

					messageBoxText = $"Se encontraron {Docentes.Count} coincidencias.";
					caption = "Operación Exitosa";
					MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
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