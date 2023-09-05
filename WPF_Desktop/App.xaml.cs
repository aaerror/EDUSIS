using Core.ServicioAlumnos;
using Infrastructure;
using Infrastructure.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.ViewModels;

namespace WPF_Desktop;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }


    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                ConfigureServices(services);
            }).Build();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        /**
         * INFRASTRUCTURE
         */
        services.AddSingleton<IUnitOfWork, UnitOfWork>();

        /**
         * CORE
         */
        services.AddSingleton<IServicioAlumno, ServicioAlumno>();

        /**
         * UI
         */
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<Func<Type, ViewModel>>( serviceProvider => 
            viewModelType => (ViewModel)serviceProvider.GetRequiredService(viewModelType)
        );

        /**
         * VIEW MODELS
         */
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<GestionAlumnosViewModel>();
        services.AddSingleton<RegistrarAlumnoViewModel>();

        services.AddSingleton(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost.StopAsync();

        var startUpWindow = AppHost.Services.GetRequiredService<MainWindow>();
        startUpWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost.StopAsync();

        base.OnExit(e);
    }
}