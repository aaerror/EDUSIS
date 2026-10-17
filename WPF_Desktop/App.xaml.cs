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

		services.AddScoped(provider => new StartupWindow
			{
				DataContext = provider.GetRequiredService<StartupViewModel>()
			});
		services.AddScoped(provider => new MainWindow
			{
				DataContext = provider.GetRequiredService<MainViewModel>()
			});
	}

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
	/// La ventana de acceso se muestra con <see cref="Window.ShowDialog"/> —modal y
	/// bloqueante— en lugar de <c>Show()</c> más un <c>IsVisibleChanged</c>. Así el arranque
	/// queda lineal y la condición de salida se lee de un vistazo: cuando el diálogo se cierra,
	/// o hay sesión iniciada y se abre la ventana principal, o no la hay y la aplicación termina.
	/// </para>
	/// <para>
	/// El <c>ShutdownMode</c> hay que coordinarlo igual, y <c>ShowDialog()</c> no exime de eso:
	/// una ventana modal también cuenta en <c>Application.Windows</c>, así que con el valor por
	/// defecto —<c>OnLastWindowClose</c>— el cierre de la ventana de acceso encola un
	/// <c>Shutdown()</c> antes de que este método alcance a mostrar <c>MainWindow</c>. El
	/// resultado era un login exitoso seguido de un apagado inmediato. Por eso <c>App.xaml</c>
	/// fija <c>OnExplicitShutdown</c> durante el arranque y acá se vuelve a
	/// <c>OnMainWindowClose</c> recién cuando la ventana principal ya es la ventana principal.
	/// </para>
	/// </remarks>
	protected override async void OnStartup(StartupEventArgs e)
	{
		try
		{
			await AppHost!.StartAsync();

			AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

			base.OnStartup(e);

			// Una sola raíz de DI (AppHost.Services): las ventanas y los servicios de navegación
			// comparten los Singleton, entre ellos SesionUsuario.
			var startupWindow = AppHost.Services.GetRequiredService<StartupWindow>();
			startupWindow.ShowDialog();

			// LoginUsuarioViewModel llama a ISesionUsuario.Iniciar(...) y cierra esta ventana. Si
			// se cerró sin autenticarse —con la X, o sin completar el acceso—, no hay nada que
			// mostrar.
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

			// MainWindow pasa a ser la ventana principal recién ahora: hasta acá lo era la de
			// acceso, que es la que LoginUsuarioViewModel cierra con
			// Application.Current.MainWindow.Close().
			MainWindow = mainWindow;

			// Recién con la ventana principal asignada se puede devolver el apagado al ciclo de
			// vida normal: de acá en más, cerrarla termina la aplicación.
			ShutdownMode = ShutdownMode.OnMainWindowClose;

			mainWindow.Show();
		}
		catch (Exception ex)
		{
			MostrarErrorFatal(ex);
		}
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
