using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDocentes;
using Core.Shared.DTOs.Personas.Requests;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Shared;
using WPF_Desktop.Store.NavigationStore;
using WPF_Desktop.ViewModels.Docentes;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels.Shared.Modal;

/// <summary>
/// Buscador de docentes del modal. Reemplaza las dos copias del mismo bloque que estaban escritas a
/// mano en <c>GestionDivisionesView</c> (para asignar preceptor) y en
/// <c>GestionSituacionRevistaView</c> (para designar en una cátedra).
/// </summary>
/// <remarks>
/// Se cierra solo: tiene el <c>ModalWindowNavigationStore</c> y lo vacía al elegir o al cancelar. No
/// hace falta un comando de cierre en <c>MainViewModel</c>, porque la ventana no decide cuándo
/// termina el contenido del modal; el contenido sí.
///
/// Un término vacío lista a todos los docentes activos en lugar de no hacer nada: así el único botón
/// sirve para buscar y para ver la nómina, que antes eran dos ("Buscar" y "Ver docentes").
///
/// No avisa por cuadro de mensaje cuántas coincidencias hubo. La grilla está a la vista y su estado
/// vacío lo dice; un cuadro que hay que cerrar para ver el resultado que ya está en pantalla es el
/// mismo ruido que se le sacó al aviso del ingreso. Los errores del servicio sí se avisan.
/// </remarks>
internal partial class BuscarViewModel : ObservableObject
{
	private readonly IServicioDocente _servicioDocente;
	private readonly IDialogService _dialogService;
	private readonly ModalWindowNavigationStore _modalNavigationStore;
	private readonly IMessenger _messenger;

	[ObservableProperty]
	private string _termino = string.Empty;

	[ObservableProperty]
	private ObservableCollection<LegajoDocenteViewModel> _resultados = new();

	[NotifyCanExecuteChangedFor(nameof(ElegirCommand))]
	[ObservableProperty]
	private LegajoDocenteViewModel _seleccionado = null;

	public IAsyncRelayCommand BuscarCommandAsync { get; }
	public IRelayCommand ElegirCommand { get; }
	public IRelayCommand CancelarCommand { get; }


	public BuscarViewModel(IServicioDocente servicioDocente,
						   IDialogService dialogService,
						   ModalWindowNavigationStore modalNavigationStore,
						   IMessenger messenger)
	{
		_servicioDocente = servicioDocente;
		_dialogService = dialogService;
		_modalNavigationStore = modalNavigationStore;
		_messenger = messenger;

		BuscarCommandAsync = new AsyncRelayCommand(ExecuteBuscarCommandAsync);
		ElegirCommand = new RelayCommand(ExecuteElegirCommand, CanExecuteElegirCommand);
		CancelarCommand = new RelayCommand(ExecuteCancelarCommand);
	}

	#region BuscarCommandAsync
	private async Task ExecuteBuscarCommandAsync()
	{
		try
		{
			var termino = Termino?.Trim() ?? string.Empty;

			var response = termino.Length is 0
				? await _servicioDocente.ListarDocentesActivosAsync()
				: await _servicioDocente.BuscarDocenteSegunNombreCompletoAsync(new NombreCompletoRequest(termino));

			Seleccionado = null;
			Resultados = response is null
				? new ObservableCollection<LegajoDocenteViewModel>()
				: new ObservableCollection<LegajoDocenteViewModel>(response.Select(docente => new LegajoDocenteViewModel(docente)));
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la búsqueda");
		}
	}
	#endregion

	#region ElegirCommand
	private bool CanExecuteElegirCommand() =>
		Seleccionado is not null;

	private void ExecuteElegirCommand()
	{
		_messenger.Send(new DocenteElegidoMessage(Seleccionado));
		Cerrar();
	}
	#endregion

	#region CancelarCommand
	private void ExecuteCancelarCommand() =>
		Cerrar();
	#endregion

	/// <summary>Vacía el store del modal, que es lo que lo oculta.</summary>
	private void Cerrar()
	{
		Termino = string.Empty;
		Seleccionado = null;
		Resultados = new ObservableCollection<LegajoDocenteViewModel>();

		_modalNavigationStore.Cerrar();
	}
}
