using CommunityToolkit.Mvvm.Messaging;
using Core.Shared;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using System.Windows;
using System;
using WPF_Desktop.Security;
using WPF_Desktop.Shared;
using WPF_Desktop.Store.NavigationStore;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Shared.Messages;
using WPF_Desktop.ViewModels;
using WPF_Desktop.Windows;

namespace WPF_Desktop;

public partial class App : Application
{
	public static IHost? AppHost { get; private set; }


	public App()
	{
		AppHost = Host.CreateDefaultBuilder()
			.ConfigureLogging(configuration =>
				{
					configuration.SetMinimumLevel(LogLevel.Debug);
					configuration.AddDebug();
				})
			.ConfigureServices((hostContext, services) =>
				{
					ConfigureServices(services);
				})
			.Build();
	}

	private void ConfigureServices(IServiceCollection services)
	{
		#region Infrastructure
		InfrastructureDI.AddInfrastructure(services);
		#endregion

		#region Core
		CoreDI.AddServices(services);
		#endregion

		#region UI
		WPF_DesktopDI.AddWPFDesktopDI(services);
		#endregion

		// Transient y no Scoped: el cambio de usuario resuelve las DOS ventanas una vez por sesión,
		// y en WPF una Window cerrada es terminal —Show() sobre ella tira
		// InvalidOperationException—. Con Scoped resuelto del proveedor raíz había una instancia por
		// proceso, que es exactamente lo que impedía volver a la pantalla de acceso.
		//
		// De paso cierra UI-23 de docs/todos.md: eran Scoped resueltas de la raíz, así que con
		// DOTNET_ENVIRONMENT=Development —donde Host.CreateDefaultBuilder activa ValidateScopes—
		// GetRequiredService<MainWindow>() lanzaba al arrancar.
		services.AddTransient(provider => new StartupWindow
			{
				DataContext = provider.GetRequiredService<StartupViewModel>()
			});
		services.AddTransient(provider => new MainWindow
			{
				DataContext = provider.GetRequiredService<MainViewModel>()
			});
	}

	/// <summary>
	/// Hay un cambio de usuario pedido y la ventana principal se está cerrando por eso. Lo escribe
	/// <see cref="TerminarSesion"/> y lo lee <see cref="OnVentanaPrincipalCerrada"/>.
	/// </summary>
	/// <remarks>
	/// Es un campo y no un parámetro porque el pedido y la decisión están separados por el cierre de
	/// una ventana: quien pide no puede esperar a que termine, y <c>Closed</c> no lleva carga. El
	/// valor por omisión —<see langword="false"/>— es el seguro: un cierre que nadie explicó es la X
	/// de la ventana, y la X apaga.
	/// </remarks>
	private bool _volverAlAcceso;

	/// <summary>
	/// Arranque de la aplicación: primero el login, después la ventana principal.
	/// </summary>
	/// <remarks>
	/// <para>
	/// El orden importa y no es decorativo. Con el RBAC funcional, las entradas del menú de
	/// <c>MainViewModel</c> se habilitan por permiso, y los permisos salen de la sesión. Si
	/// <c>MainWindow</c> se abriera antes del login, su ViewModel se construiría sin sesión y el
	/// menú quedaría deshabilitado: es exactamente el síntoma que estaba registrado como UI-39
	/// mientras este bloque estuvo comentado.
	/// </para>
	/// <para>
	/// Desde que existe el cambio de usuario el arranque ya no es lineal sino un CICLO, y por eso el
	/// cuerpo vive en <see cref="AbrirCicloDeSesion"/>: lo que antes pasaba una vez al arrancar
	/// ahora pasa una vez por sesión. Lo único que queda acá es lo que pasa una vez por PROCESO —el
	/// host, la política de principal, la suscripción al mensajero—.
	/// </para>
	/// </remarks>
	protected override async void OnStartup(StartupEventArgs e)
	{
		try
		{
			await AppHost!.StartAsync();

			AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

			base.OnStartup(e);

			// Las dos salidas del menú —cerrar la sesión y salir— llegan por acá. MainViewModel
			// declara la intención y no sabe cómo se cumple; la decisión de qué pasa después de
			// cerrar la ventana principal es del ciclo de vida de la aplicación.
			AppHost.Services
				   .GetRequiredService<IMessenger>()
				   .Register<App, FinDeSesionMessage>(this, (app, mensaje) => app.TerminarSesion(mensaje));

			AbrirCicloDeSesion();
		}
		catch (Exception ex)
		{
			MostrarErrorFatal(ex);
		}
	}

	/// <summary>
	/// Una sesión completa: pantalla de acceso y, si alguien entra, ventana principal.
	/// </summary>
	/// <remarks>
	/// <para>
	/// La ventana de acceso se muestra con <see cref="Window.ShowDialog"/> —modal y bloqueante— en
	/// lugar de <c>Show()</c> más un <c>IsVisibleChanged</c>. Así el acceso queda lineal y la
	/// condición de salida se lee de un vistazo: cuando el diálogo se cierra, o hay sesión iniciada
	/// y se abre la ventana principal, o no la hay y la aplicación termina.
	/// </para>
	/// <para>
	/// El <c>ShutdownMode</c> hay que coordinarlo igual, y <c>ShowDialog()</c> no exime de eso: una
	/// ventana modal también cuenta en <c>Application.Windows</c>, así que con el valor por defecto
	/// —<c>OnLastWindowClose</c>— el cierre de la ventana de acceso encola un <c>Shutdown()</c>
	/// antes de que este método alcance a mostrar <c>MainWindow</c>. El resultado era un login
	/// exitoso seguido de un apagado inmediato. Por eso <c>App.xaml</c> fija
	/// <c>OnExplicitShutdown</c>.
	/// </para>
	/// <para>
	/// Y <c>OnExplicitShutdown</c> se queda puesto para siempre, que es el cambio que trajo el
	/// relogin. Antes este método devolvía el modo a <c>OnMainWindowClose</c> una vez abierta la
	/// ventana principal; con el cambio de usuario eso apagaría la aplicación justamente cuando se
	/// cierra la ventana para volver al acceso. Ahora el único que apaga es
	/// <see cref="OnVentanaPrincipalCerrada"/>, y lo hace leyendo
	/// <see cref="_volverAlAcceso"/> —un solo lugar decide, con el motivo del cierre a la vista—.
	/// </para>
	/// </remarks>
	private void AbrirCicloDeSesion()
	{
		// Antes de pedir credenciales, no después: así el estado del usuario anterior no existe ya
		// cuando la pantalla de acceso se dibuja, y no hay una ventana en la que pueda alcanzar a
		// verse. En el primer arranque no limpia nada y corre igual, que es lo que lo hace seguro.
		LimpiarEstadoDeSesion();

		// Una sola raíz de DI (AppHost.Services): las ventanas y los servicios de navegación
		// comparten los Singleton, entre ellos SesionUsuario.
		var startupWindow = AppHost!.Services.GetRequiredService<StartupWindow>();

		// La ventana de acceso es la ventana principal MIENTRAS dura el acceso, y esto no es
		// cosmético: LoginUsuarioViewModel cierra su propia ventana con
		// Application.Current.MainWindow.Close(). En el primer arranque WPF lo asigna solo —la de
		// acceso es la primera Window del proceso—, pero en un cambio de usuario no: MainWindow
		// seguiría apuntando a la ventana principal YA CERRADA, y Close() sobre una ventana cerrada
		// es un no-op silencioso. El acceso se quedaría abierto para siempre con la sesión ya
		// iniciada, y el usuario sin forma de llegar al menú.
		MainWindow = startupWindow;
		startupWindow.ShowDialog();

		// ShowDialog() vuelve cuando la ventana ya se cerró. Soltar acá la suscripción del
		// StartupViewModel es el equivalente del Desconectar() de MainViewModel: el
		// StartupWindowNavigationStore es Singleton con un event fuerte, así que sin esto quedaría
		// un StartupViewModel colgado de él por cada sesión.
		(startupWindow.DataContext as StartupViewModel)?.Desconectar();

		// LoginUsuarioViewModel llama a ISesionUsuario.Iniciar(...) y cierra esta ventana. Si se
		// cerró sin autenticarse —con la X, o sin completar el acceso—, no hay nada que mostrar.
		// Vale igual para un cambio de usuario: quien pidió cerrar la sesión y después cancela el
		// acceso dejó la máquina sin nadie adentro, y lo correcto es apagar y no volver a la sesión
		// que ya se cerró.
		var sesion = AppHost.Services.GetRequiredService<ISesionUsuario>();
		if (!sesion.EstaAutenticado)
		{
			Shutdown(0);
			return;
		}

		// Sin navegación inicial: MainWindow ya recibe su MainViewModel como DataContext desde
		// la factory de DI. Navegar acá con el INavigationService por defecto
		// —MainNavigationService<MainViewModel>— dejaba el propio MainViewModel en el
		// MainWindowNavigationStore, y el ContentControl de MainWindow lo resolvía con el
		// DataTemplate que instancia <windows:MainWindow/>: una Window dentro de un Visual.
		// El área de contenido arranca vacía hasta que el usuario elige una entrada del menú.
		var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();

		// MainWindow pasa a ser la ventana principal recién ahora: hasta acá lo era la de acceso.
		MainWindow = mainWindow;

		mainWindow.Closed += OnVentanaPrincipalCerrada;
		mainWindow.Show();
	}

	/// <summary>
	/// Alguien pidió terminar la sesión desde el menú. Anota si hay que volver al acceso y cierra la
	/// ventana principal; <see cref="OnVentanaPrincipalCerrada"/> hace el resto.
	/// </summary>
	/// <remarks>
	/// No apaga ni reabre nada acá a propósito. Mientras este método corre, la ventana principal
	/// todavía está abierta y su <c>MainViewModel</c> está en medio de la ejecución de un comando:
	/// levantar la pantalla de acceso con <c>ShowDialog()</c> en ese punto la anidaría encima de la
	/// ventana que se va a cerrar. El cierre es el único efecto, y el <c>Closed</c> es el punto en
	/// que la sesión anterior ya terminó de verdad.
	/// </remarks>
	private void TerminarSesion(FinDeSesionMessage mensaje)
	{
		_volverAlAcceso = mensaje.VolverAlAcceso;

		// Sin ventana principal el mensaje llegó fuera de tiempo —no hay sesión que terminar—.
		MainWindow?.Close();
	}

	/// <summary>
	/// La ventana principal se cerró: o empieza otra sesión, o termina la aplicación.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Es el único lugar que apaga, y por eso <see cref="ShutdownMode"/> se queda en
	/// <c>OnExplicitShutdown</c>: si WPF apagara solo al cerrarse esta ventana, el cambio de usuario
	/// no tendría dónde ocurrir.
	/// </para>
	/// <para>
	/// Los tres caminos de cierre entran por acá y se distinguen por <see cref="_volverAlAcceso"/>:
	/// «Cerrar la sesión» lo deja en <see langword="true"/>, «Salir» en <see langword="false"/>, y
	/// la X de la ventana no pasa por el mensajero, así que encuentra el <see langword="false"/> por
	/// omisión. Que la X y «Salir» terminen en el mismo lado es intencional: las dos dicen lo mismo.
	/// </para>
	/// </remarks>
	private void OnVentanaPrincipalCerrada(object? sender, EventArgs e)
	{
		if (sender is Window ventana)
		{
			ventana.Closed -= OnVentanaPrincipalCerrada;
		}

		if (!_volverAlAcceso)
		{
			Shutdown(0);
			return;
		}

		// Se consume el pedido antes de atenderlo: si la sesión siguiente se cerrara con la X, el
		// flag tiene que estar de vuelta en false.
		_volverAlAcceso = false;

		// Por el Dispatcher y no con una llamada directa. AbrirCicloDeSesion hace ShowDialog(), que
		// bombea mensajes hasta que el acceso se cierre: llamarlo desde acá lo anidaría DENTRO del
		// Closed de la ventana anterior, y cada cambio de usuario dejaría apilados un frame del
		// Dispatcher y un marco de pila que no se liberan hasta que termine la sesión más nueva. Con
		// BeginInvoke este handler termina primero y el ciclo arranca con la pila limpia.
		Dispatcher.BeginInvoke(new Action(AbrirCicloDeSesion));
	}

	/// <summary>
	/// Devuelve la sesión y todo el estado compartido de la interfaz al punto en que arrancó el
	/// proceso, para que el usuario que entra después no herede nada del anterior.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Hace falta porque los <c>*Store</c> son <c>Singleton</c> —uno por proceso— y la sesión ahora
	/// dura menos que el proceso. Sin este barrido el usuario que entra encuentra el legajo, el
	/// curso, la división, la materia y la cátedra que dejó seleccionados el anterior: no es sólo
	/// prolijidad, <c>LegajoStore</c> guarda a una persona concreta.
	/// </para>
	/// <para>
	/// Los stores de valor se barren por <c>IEnumerable&lt;IEstadoDeSesion&gt;</c> sin nombrar a
	/// ninguno, así que un store nuevo entra al barrido registrándose en
	/// <c>WPF_DesktopDI.AddEstadoDeSesion</c> y no editando este método. Los tres
	/// <c>*NavigationStore</c> van aparte y en este orden: lo que guardan no es un valor del usuario
	/// sino el ViewModel activo de una ventana, y el modal se cierra ANTES que el contenido porque
	/// un modal abierto sobre un área de contenido vacía es un estado que ninguna pantalla sabe
	/// dibujar.
	/// </para>
	/// </remarks>
	private void LimpiarEstadoDeSesion()
	{
		AppHost!.Services.GetRequiredService<ISesionUsuario>().Cerrar();

		foreach (var estado in AppHost.Services.GetServices<IEstadoDeSesion>())
		{
			estado.Limpiar();
		}

		AppHost.Services.GetRequiredService<ModalWindowNavigationStore>().Cerrar();
		AppHost.Services.GetRequiredService<MainWindowNavigationStore>().ViewModel = null!;
		AppHost.Services.GetRequiredService<StartupWindowNavigationStore>().ViewModel = null!;
	}

	protected override async void OnExit(ExitEventArgs e)
	{
		try
		{
			await AppHost!.StopAsync();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error al detener el host: {ex}");
		}

		base.OnExit(e);
	}

	private void MostrarErrorFatal(Exception ex)
	{
		string caption = "Error en la aplicación";
		string message = $"Ha ocurrido un error inesperado y la aplicación se debe cerrar.\nDisculpe las molestias ocasionadas.\n\nError: { ex.Message }";
		MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);

		Shutdown(-1);
	}

	private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
	{
		string caption = "Error en la aplicación";
		string message = $"Ha ocurrido un error inesperado y la aplicación se debe cerrar.\nDisculpe las molestias ocasionadas.\n\nError: { e.Exception.Message }";
		MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
		e.Handled = true;

		Shutdown(-1);
	}
}
