using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDocumentos;
using Core.ServicioUsuarios;
using Domain.Usuarios;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Security;
using WPF_Desktop.Store.NavigationStore;
using WPF_Desktop.ViewModels.Usuarios;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels;

internal partial class MainViewModel : ObservableRecipient, IRecipient<UsuarioLogeadoMessage>
{
	private readonly IServicioUsuario _servicioUsuario;
	private readonly IServicioDocumento _servicioDocumento;
	private readonly ISesionUsuario _sesion;

	private MainWindowNavigationStore _navigationStore;
	private ModalWindowNavigationStore _modalNavigationStore;

	public ObservableObject ViewModelActual => _navigationStore.ViewModel;
	public ObservableObject ModalViewModelActual => _modalNavigationStore.ViewModel;

	public bool ModalEstaAbierto => _modalNavigationStore.EstaAbierto;

	[ObservableProperty]
	private UsuarioViewModel _usuarioViewModel;

	#region Commands
	public IRelayCommand GestionDocentesCommand { get; }
	public IRelayCommand GestionAlumnosCommand { get; }
	public IRelayCommand GestionCursosCommand { get; }
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
						 IMessenger messenger)
		: base(messenger)
	{
		_navigationStore = navigationStore;
		_modalNavigationStore = modalNavigationStore;
		_servicioUsuario = servicioUsuario;
		_servicioDocumento = servicioDocumento;
		_sesion = sesion;

		// Cada entrada del menú se habilita por PERMISO, no por rol: mover una atribución es un
		// cambio en Domain.Usuarios.Rol y no acá. Sin sesión iniciada no hay permisos, así que
		// las tres quedan deshabilitadas por omisión.
		GestionDocentesCommand = new RelayCommand(() => docentesNavigationService.Navigate(),
												  () => _sesion.TienePermiso(Permiso.GestionarDocentes));
		GestionAlumnosCommand = new RelayCommand(() => alumnosNavigationService.Navigate(),
												 () => _sesion.TienePermiso(Permiso.GestionarAlumnos));
		GestionCursosCommand = new RelayCommand(() => cursosNavigationService.Navigate(),
												() => _sesion.TienePermiso(Permiso.GestionarCursos));
		CargarUsuarioAsyncCommand = new AsyncRelayCommand(CargarUsuarioAsync);

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

	private void OnViewModelChanged() =>
		OnPropertyChanged(nameof(ViewModelActual));

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