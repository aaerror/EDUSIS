using Core.Shared;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using System.Windows;
using System;
using WPF_Desktop.Navigation;
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

	protected override async void OnStartup(StartupEventArgs e)
	{
		try
		{
			await AppHost!.StartAsync();

			AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
			//AppDomain.CurrentDomain.SetThreadPrincipal(new CustomPrincipal(new AnonymousIdentity()));

			base.OnStartup(e);

			// Una sola raíz de DI (AppHost.Services): la ventana y el servicio de navegación comparten los Singleton.
			var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
			INavigationService navigationService = AppHost.Services.GetRequiredService<INavigationService>();
			navigationService.Navigate();
			mainWindow.Show();

			/*
			Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(""), null);

			var startUpWindow = AppHost.Services.GetRequiredService<AccesoWindow>();
			startUpWindow.Show();

			startUpWindow.IsVisibleChanged += (s, ev) =>
			{
				if (!startUpWindow.IsVisible && startUpWindow.IsLoaded)
				{
					var mainWindow = new MainWindow();
					mainWindow.Show();
				}
			};
			*/

			/*
			var startupWindow = AppHost.Services.GetService<StartupWindow>();
			startupWindow.Show();

			startupWindow.IsVisibleChanged += (s, ev) =>
			{
				if (!startupWindow.IsVisible && startupWindow.IsLoaded)
				{
					var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
					INavigationService navigationService = AppHost.Services.GetRequiredService<INavigationService>();
					navigationService.Navigate();
					mainWindow.Show();
				}
			};
			*/
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
