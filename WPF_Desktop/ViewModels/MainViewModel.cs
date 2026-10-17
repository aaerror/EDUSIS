using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDocumentos;
using Core.ServicioUsuarios;
using Domain.Usuarios;
using System.Threading.Tasks;
using System.Windows;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Security;
using WPF_Desktop.Shared;
using WPF_Desktop.Store.NavigationStore;
using WPF_Desktop.ViewModels.Usuarios;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels;

internal partial class MainViewModel : ObservableRecipient, IRecipient<UsuarioLogeadoMessage>
{
	private readonly IServicioUsuario _servicioUsuario;
	private readonly IServicioDocumento _servicioDocumento;
	private readonly ISesionUsuario _sesion;
	private readonly IDialogService _dialogos;

	private MainWindowNavigationStore _navigationStore;
	private ModalWindowNavigationStore _modalNavigationStore;

	public ObservableObject ViewModelActual => _navigationStore.ViewModel;
	public ObservableObject ModalViewModelActual => _modalNavigationStore.ViewModel;

	public bool ModalEstaAbierto => _modalNavigationStore.EstaAbierto;

	/// <summary>
	/// Hay una sección abierta. Mientras sea falso, la ventana muestra la bienvenida en lugar
	/// de un área en blanco.
	/// </summary>
	public bool HayContenido => _navigationStore.ViewModel is not null;

	/// <summary>
	/// Nombre de la sección abierta, para la barra de título. Es la única pista de "dónde
	/// estoy" fuera del propio contenido.
	/// </summary>
	[ObservableProperty]
	private string _seccionActual = string.Empty;

	[ObservableProperty]
	private UsuarioViewModel _usuarioViewModel;

	#region Commands
	public IRelayCommand GestionDocentesCommand { get; }
	public IRelayCommand GestionAlumnosCommand { get; }
	public IRelayCommand GestionCursosCommand { get; }
	public IRelayCommand SalirCommand { get; }
	#endregion

	#region AsyncCommands
	public IAsyncRelayCommand CargarUsuarioAsyncCommand { get; }
	#endregion


	public MainViewModel(MainWindowNavigationStore navigationStore,
						 ModalWindowNavigationStore modalNavigationStore,
						 IServicioUsuario servicioUsuario,
						 IServicioDocumento servicioDocumento,
						 INavigationService docentesNavigationService,
						 INavigationService alumnosNavigationService,
						 INavigationService cursosNavigationService,
						 ISesionUsuario sesion,
						 IDialogService dialogos,
						 IMessenger messenger)
		: base(messenger)
	{
		_navigationStore = navigationStore;
		_modalNavigationStore = modalNavigationStore;
		_servicioUsuario = servicioUsuario;
		_servicioDocumento = servicioDocumento;
		_sesion = sesion;
		_dialogos = dialogos;

		// Cada entrada del menú se habilita por PERMISO, no por rol: mover una atribución es un
		// cambio en Domain.Usuarios.Rol y no acá. Sin sesión iniciada no hay permisos, así que
		// las tres quedan deshabilitadas por omisión.
		GestionDocentesCommand = new RelayCommand(() => Navegar(docentesNavigationService, "Docentes"),
												  () => _sesion.TienePermiso(Permiso.GestionarDocentes));
		GestionAlumnosCommand = new RelayCommand(() => Navegar(alumnosNavigationService, "Alumnos"),
												 () => _sesion.TienePermiso(Permiso.GestionarAlumnos));
		GestionCursosCommand = new RelayCommand(() => Navegar(cursosNavigationService, "Cursos"),
												() => _sesion.TienePermiso(Permiso.GestionarCursos));
		CargarUsuarioAsyncCommand = new AsyncRelayCommand(CargarUsuarioAsync);
		SalirCommand = new RelayCommand(Salir);

		_navigationStore.ViewModelChanged += OnViewModelChanged;
		_modalNavigationStore.ViewModelChanged += OnModalViewModelChanged;
		_sesion.Cambio += OnSesionCambio;

		Messenger.Register<UsuarioLogeadoMessage>(this);
		_servicioDocumento.GenerarCertificadoAlumnoRegular();
	}

	private async Task CargarUsuarioAsync()
	{
		// https://formatexception.com/category/mvvm/
		UsuarioViewModel = new UsuarioViewModel(_servicioUsuario, _sesion);
	}

	/// <summary>
	/// Al iniciar o cerrar sesión los permisos cambian, pero <c>RelayCommand</c> no reevalúa su
	/// <c>CanExecute</c> por su cuenta: hay que avisarle. Sin esto el menú queda congelado en el
	/// estado que tenía al construirse el ViewModel —es decir, todo deshabilitado—.
	/// </summary>
	private void OnSesionCambio()
	{
		GestionDocentesCommand.NotifyCanExecuteChanged();
		GestionAlumnosCommand.NotifyCanExecuteChanged();
		GestionCursosCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Cierra la sesión y la aplicación.
	/// </summary>
	/// <remarks>
	/// Cierra las dos cosas, no sólo la sesión: volver a la pantalla de acceso sin salir
	/// exigiría poder reabrir <c>StartupWindow</c> y reconstruir <c>MainWindow</c>, y las dos
	/// están registradas una sola vez por proceso —una ventana cerrada no se puede volver a
	/// mostrar—. Queda anotado como pendiente de aprobación.
	/// </remarks>
	private void Salir()
	{
		if (!_dialogos.Confirmar("¿Cerrar sesión y salir de EDUSIS?", "Salir"))
		{
			return;
		}

		_sesion.Cerrar();
		Application.Current.Shutdown(0);
	}

	/// <summary>
	/// Navega y deja registrado el nombre de la sección para la barra de título.
	/// </summary>
	private void Navegar(INavigationService servicio, string seccion)
	{
		servicio.Navigate();
		SeccionActual = seccion;
	}

	private void OnViewModelChanged()
	{
		OnPropertyChanged(nameof(ViewModelActual));
		OnPropertyChanged(nameof(HayContenido));
	}

	private void OnModalViewModelChanged()
	{
		OnPropertyChanged(nameof(ModalViewModelActual));
		OnPropertyChanged(nameof(ModalEstaAbierto));
	}

	#region Message
	//protected override void OnActivated()
	//{
	//	WeakReferenceMessenger.Default.Register<UsuarioLogeadoMessage>(this);
	//}

	//protected override void OnDeactivated()
	//{
	//	WeakReferenceMessenger.Default.UnregisterAll(this);
	//}

	public void Receive(UsuarioLogeadoMessage message)
	{
		Console.WriteLine(message);
	}
	#endregion
}