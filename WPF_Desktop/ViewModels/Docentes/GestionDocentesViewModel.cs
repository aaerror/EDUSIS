using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels.Docentes;

internal partial class GestionDocentesViewModel : ObservableValidator
{
	#region Servicios
	private readonly IServicioDocente _servicioDocentes;
	#endregion

	#region NavigationService
	private readonly INavigationService _registrarDocenteNavigationService;
	private readonly INavigationService _perfilDocenteNavigationService;
	private readonly INavigationService _gestionPuestosNavigationService;
	private readonly INavigationService _gestionLicenciasNavigationService;
	#endregion

	private LegajoStore _perfilBuscadoStore;

	/// <summary>
	/// Texto del filtro. No lleva <c>[Required]</c>: la pantalla arranca con el listado
	/// completo y el filtro es opcional.
	/// </summary>
	/// <remarks>
	/// El atributo anterior tenía un efecto que no se veía venir: con el campo vacío
	/// —es decir, al abrir la pantalla— <c>HasErrors</c> quedaba en <c>true</c>, y como
	/// <c>CanExecuteBuscarCommand</c> era <c>!HasErrors</c>, el botón de buscar nacía
	/// deshabilitado. Había que escribir algo para poder hacer cualquier cosa.
	/// </remarks>
	[ObservableProperty]
	private string _searchedText = string.Empty;

	[ObservableProperty]
	private bool _cargando;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	private LegajoDocenteViewModel? _legajoDocenteSeleccionado;

	[ObservableProperty]
	private ObservableCollection<LegajoDocenteViewModel> _legajosDocentes = new();

	/// <summary>
	/// Vista filtrada del listado. El filtro se aplica en memoria sobre lo ya cargado, así
	/// que responde a cada tecla sin ir a la base.
	/// </summary>
	public ICollectionView DocentesFiltrados { get; }

	/// <summary>Resumen del índice: "24 docentes" o "3 de 24".</summary>
	public string Resumen
	{
		get
		{
			int total = LegajosDocentes.Count;
			if (total is 0)
			{
				return Cargando ? "Cargando…" : "Sin docentes activos";
			}

			int visibles = 0;
			foreach (var _ in DocentesFiltrados)
			{
				visibles++;
			}

			return visibles == total
				? $"{ total } { (total is 1 ? "docente" : "docentes") }"
				: $"{ visibles } de { total }";
		}
	}

	#region Commands
	public IRelayCommand RegistrarCommand { get; }
	public IAsyncRelayCommand CargarCommandAsync { get; }
	public IAsyncRelayCommand EliminarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	#endregion


	public GestionDocentesViewModel(IServicioDocente servicioDocentes,
									INavigationService registrarDocenteNavigationService,
									INavigationService perfilDocenteNavigationService,
									INavigationService gestionPuestoNavigationService,
									INavigationService gestionLicenciasNavigationService,
									LegajoStore perfilBuscadoStore)
	{
		_servicioDocentes = servicioDocentes;
		_registrarDocenteNavigationService = registrarDocenteNavigationService;
		_perfilDocenteNavigationService = perfilDocenteNavigationService;
		_gestionPuestosNavigationService = gestionPuestoNavigationService;
		_gestionLicenciasNavigationService = gestionLicenciasNavigationService;
		_perfilBuscadoStore = perfilBuscadoStore;

		DocentesFiltrados = CollectionViewSource.GetDefaultView(LegajosDocentes);
		DocentesFiltrados.Filter = CoincideConFiltro;

		RegistrarCommand = new RelayCommand<string>(_ => _registrarDocenteNavigationService.Navigate());
		CargarCommandAsync = new AsyncRelayCommand(ExecuteCargarCommandAsync);
		EliminarCommand = new AsyncRelayCommand(ExecuteEliminarCommandAsync, CanExecuteEliminarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);
	}

	#region Filtro
	/// <summary>
	/// Coincidencia por nombre o por legajo, sin distinguir mayúsculas ni acentos de más: el
	/// personal de secretaría busca tanto por apellido como por número de legajo.
	/// </summary>
	private bool CoincideConFiltro(object item)
	{
		if (string.IsNullOrWhiteSpace(SearchedText))
		{
			return true;
		}

		if (item is not LegajoDocenteViewModel docente)
		{
			return false;
		}

		string filtro = SearchedText.Trim();

		// Ojo con los nombres: en LegajoDocenteViewModel la región "Properties" escrita a mano
		// —con DNI, CUIL y compañía— está dentro de un /* */, así que las propiedades reales
		// son las que genera [ObservableProperty] a partir de los campos.
		return docente.NombreCompleto.Contains(filtro, StringComparison.OrdinalIgnoreCase)
			|| docente.Legajo.Contains(filtro, StringComparison.OrdinalIgnoreCase)
			|| docente.DocumentoNacionalIdentidad.Contains(filtro, StringComparison.OrdinalIgnoreCase);
	}

	partial void OnSearchedTextChanged(string value)
	{
		DocentesFiltrados.Refresh();
		OnPropertyChanged(nameof(Resumen));
	}

	partial void OnCargandoChanged(bool value) =>
		OnPropertyChanged(nameof(Resumen));
	#endregion

	#region CargarCommand
	/// <summary>
	/// Trae el listado completo de docentes activos al abrir la pantalla.
	/// </summary>
	/// <remarks>
	/// <c>IServicioDocente.ListarDocentesActivosAsync</c> ya existía y no la llamaba nadie: la
	/// pantalla obligaba a escribir un nombre y apretar "Buscar" antes de mostrar una sola
	/// fila. Eso convierte el índice en una adivinanza —hay que saber a quién se busca para
	/// poder ver quién hay— y es el motivo principal por el que la pantalla se sentía vacía.
	/// </remarks>
	private async Task ExecuteCargarCommandAsync()
	{
		Cargando = true;

		try
		{
			var response = await _servicioDocentes.ListarDocentesActivosAsync();

			LegajosDocentes.Clear();
			foreach (var legajoDocente in response)
			{
				LegajosDocentes.Add(new LegajoDocenteViewModel(legajoDocente));
			}

			DocentesFiltrados.Refresh();
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "No se pudo cargar el listado de docentes", MessageBoxButton.OK, MessageBoxImage.Error);
		}
		finally
		{
			Cargando = false;
			OnPropertyChanged(nameof(Resumen));
		}
	}
	#endregion

	#region EliminarCommand
	private bool CanExecuteEliminarCommand() =>
		LegajoDocenteSeleccionado is not null;

	private async Task ExecuteEliminarCommandAsync()
	{
		if (LegajoDocenteSeleccionado is null)
		{
			return;
		}

		string nombre = LegajoDocenteSeleccionado.NombreCompleto;

		var result = MessageBox.Show($"¿Quitar a { nombre } del listado de docentes?",
									 "Quitar docente",
									 MessageBoxButton.YesNo,
									 MessageBoxImage.Question);
		if (result is not MessageBoxResult.Yes)
		{
			return;
		}

		try
		{
			await _servicioDocentes.QuitarDocenteAsync(new DocenteIDRequest(LegajoDocenteSeleccionado.DocenteID));
			LegajoDocenteSeleccionado = null;

			// Recargar: dejar la fila borrada en pantalla es la forma más rápida de que
			// alguien vuelva a operar sobre un registro que ya no existe.
			await ExecuteCargarCommandAsync();

			MessageBox.Show($"Se quitó a { nombre }.", "Docente quitado", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "No se pudo quitar el docente", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
	#endregion

	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj) =>
		LegajoDocenteSeleccionado is not null;

	private void ExecuteNavigationCommand(object obj)
	{
		if (LegajoDocenteSeleccionado is null)
		{
			return;
		}

		_perfilBuscadoStore.PersonaID = LegajoDocenteSeleccionado.DocenteID;
		_perfilBuscadoStore.Documento = LegajoDocenteSeleccionado.DocumentoNacionalIdentidad;

		switch (obj)
		{
			case "Licencia":
				_gestionLicenciasNavigationService.Navigate();
				break;
			case "Perfil":
				WeakReferenceMessenger.Default.Send(new DocenteSeleccionadoMessage(_perfilBuscadoStore.PersonaID));
				_perfilDocenteNavigationService.Navigate();
				break;
			case "Puesto":
				_gestionPuestosNavigationService.Navigate();
				break;
		}
	}
	#endregion
}
