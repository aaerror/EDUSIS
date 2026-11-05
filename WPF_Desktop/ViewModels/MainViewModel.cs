using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioUsuarios;
using Domain.Usuarios;
using System.Collections.ObjectModel;
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
	private readonly ISesionUsuario _sesion;
	private readonly IDialogService _dialogos;
	private readonly AvisoStore _avisos;

	private MainWindowNavigationStore _navigationStore;
	private ModalWindowNavigationStore _modalNavigationStore;

	public ObservableObject ViewModelActual => _navigationStore.ViewModel;
	public ObservableObject ModalViewModelActual => _modalNavigationStore.ViewModel;

	public bool ModalEstaAbierto => _modalNavigationStore.EstaAbierto;

	/// <summary>
	/// Los avisos al pie. Los escribe <c>DialogService</c> desde cualquier pantalla y los dibuja
	/// el shell, que es el único lugar de la aplicación que sobrevive a la navegación.
	/// </summary>
	public ObservableCollection<Aviso> Avisos => _avisos.Avisos;

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
	public IRelayCommand AsistenciasCommand { get; }
	public IRelayCommand GestionDocumentacionCommand { get; }
	public IRelayCommand GestionReportesCommand { get; }
	public IRelayCommand SalirCommand { get; }
	#endregion

	#region AsyncCommands
	public IAsyncRelayCommand CargarUsuarioAsyncCommand { get; }
	#endregion


	public MainViewModel(MainWindowNavigationStore navigationStore,
						 ModalWindowNavigationStore modalNavigationStore,
						 IServicioUsuario servicioUsuario,
						 INavigationService docentesNavigationService,
						 INavigationService alumnosNavigationService,
						 INavigationService cursosNavigationService,
						 INavigationService asistenciasNavigationService,
						 INavigationService documentacionNavigationService,
						 INavigationService reportesNavigationService,
						 ISesionUsuario sesion,
						 IDialogService dialogos,
						 AvisoStore avisos,
						 IMessenger messenger)
		: base(messenger)
	{
		_navigationStore = navigationStore;
		_modalNavigationStore = modalNavigationStore;
		_servicioUsuario = servicioUsuario;
		_sesion = sesion;
		_dialogos = dialogos;
		_avisos = avisos;

		// Cada entrada del menú se habilita por PERMISO, no por rol: mover una atribución es un
		// cambio en Domain.Usuarios.Rol y no acá. Sin sesión iniciada no hay permisos, así que
		// todas quedan deshabilitadas por omisión.
		GestionDocentesCommand = new RelayCommand(() => Navegar(docentesNavigationService, "Docentes"),
												  () => _sesion.TienePermiso(Permiso.GestionarDocentes));
		GestionAlumnosCommand = new RelayCommand(() => Navegar(alumnosNavigationService, "Alumnos"),
												 () => _sesion.TienePermiso(Permiso.GestionarAlumnos));
		// Cursos acepta también CargarCalificaciones: la pantalla de calificaciones es un
		// drill-down de Cursos → Divisiones → Cursantes, así que exigir sólo GestionarCursos deja
		// a Preceptoría autorizada por Core para cargar notas y sin camino para llegar a hacerlo.
		// Es el mismo defecto de forma que la raíz Asistencias cerró para TomarAsistencia,
		// resuelto acá con el OR de Documentación y Reportes en vez de con una raíz nueva.
		// Contrapartida asumida: Preceptoría ve el árbol entero de Cursos —registrar curso,
		// currículas, cátedras— y Core le va a rechazar esas operaciones una por una, porque los
		// comandos de las pantallas no consultan permisos: sólo el menú.
		//
		// Preceptoría es hoy el único rol que necesita este OR. Docente también carga
		// calificaciones, pero con el CRUD total sobre lo académico tiene GestionarCursos propio,
		// así que para ese rol la segunda condición es redundante. No se quita: lo que gobierna la
		// raíz es el permiso, no el rol, y mover una atribución no debería apagar una pantalla.
		GestionCursosCommand = new RelayCommand(() => Navegar(cursosNavigationService, "Cursos"),
												() => _sesion.TienePermiso(Permiso.GestionarCursos)
												   || _sesion.TienePermiso(Permiso.CargarCalificaciones));
		AsistenciasCommand = new RelayCommand(() => Navegar(asistenciasNavigationService, "Asistencias"),
											  () => _sesion.TienePermiso(Permiso.TomarAsistencia));

		// Las dos secciones de emisión aceptan el permiso general O el de Preceptoría: el
		// certificado de asistencia (CU-30) y los reportes de cursada (CU-35/36) viven en estas
		// pantallas, y exigir sólo el permiso general dejaría a Preceptoría sin poder abrirlas
		// para emitir justamente lo que sí puede. Las operaciones de adentro siguen guardadas
		// una por una en Core: llegar a la pantalla no autoriza nada.
		GestionDocumentacionCommand = new RelayCommand(() => Navegar(documentacionNavigationService, "Documentación"),
													   () => _sesion.TienePermiso(Permiso.GenerarDocumentos)
														  || _sesion.TienePermiso(Permiso.GenerarDocumentosAsistencia));
		GestionReportesCommand = new RelayCommand(() => Navegar(reportesNavigationService, "Reportes"),
												  () => _sesion.TienePermiso(Permiso.GenerarReportes)
													 || _sesion.TienePermiso(Permiso.GenerarReportesDeCursada));
		CargarUsuarioAsyncCommand = new AsyncRelayCommand(CargarUsuarioAsync);
		SalirCommand = new RelayCommand(Salir);

		_navigationStore.ViewModelChanged += OnViewModelChanged;
		_modalNavigationStore.ViewModelChanged += OnModalViewModelChanged;
		_sesion.Cambio += OnSesionCambio;

		Messenger.Register<UsuarioLogeadoMessage>(this);
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
		AsistenciasCommand.NotifyCanExecuteChanged();
		GestionDocumentacionCommand.NotifyCanExecuteChanged();
		GestionReportesCommand.NotifyCanExecuteChanged();
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
		if (!_dialogos.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: "Cerrar la sesión y salir de EDUSIS",
				consecuencia: "Se cierra la aplicación. Lo que esté a medio cargar en la pantalla abierta se pierde: "
							+ "sólo queda guardado lo que ya confirmaste.",
				verbo: "Salir de EDUSIS")))
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