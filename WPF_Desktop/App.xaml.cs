using Core.Shared;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.ViewModels;

namespace WPF_Desktop;

public partial class App : Application
{
    private IServiceProvider _serviceProvider;
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

        services.AddScoped(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost.StopAsync();

        INavigationService navigationService = _serviceProvider.GetRequiredService<INavigationService>();
        navigationService.Navigate();

        var startUpWindow = AppHost.Services.GetRequiredService<MainWindow>();
        startUpWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost.StopAsync();

        base.OnExit(e);
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        string caption = "Ha ocurrido un error inesperado";
        string message = e.Exception.Message;
        MessageBox.Show($"Error: { message }", caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
        e.Handled = true;
    }
}