using Core.ServicioAlumnos;
using Infrastructure;
using Infrastructure.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
using WPF_Desktop.Navigation;
using WPF_Desktop.Navigation.NavigationServices;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels;

namespace WPF_Desktop;

public partial class App : Application
{
    private IServiceProvider _serviceProvider;
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
        // Stores
        services.AddSingleton<ModalNavigationStore>();
        services.AddSingleton<NavigationStore>();
        services.AddSingleton<PerfilBuscadoStore>();
        // Navigation
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INavigationService>(provider => CreateMainNavigationService(provider));

        /**
         * VIEW MODELS
         */
        services.AddTransient<MainViewModel>(provider => new MainViewModel(
            provider.GetRequiredService<ModalNavigationStore>(),
            provider.GetRequiredService<NavigationStore>(),
            CreateGestionAlumnosNavigationService(provider)));
        
        services.AddTransient<LoginViewModel>();

        services.AddTransient<GestionAlumnosViewModel>(provider => new GestionAlumnosViewModel(
            CreateRegistrarAlumnoNavigationService(provider),
            CreateVerPerfilNavigationService(provider),
            provider.GetRequiredService<PerfilBuscadoStore>(),
            provider.GetRequiredService<IServicioAlumno>()));
        
        services.AddTransient<PerfilAlumnoViewModel>(provider => new PerfilAlumnoViewModel(
            provider.GetRequiredService<IServicioAlumno>(),
            provider.GetRequiredService<PerfilBuscadoStore>()));
        
        services.AddTransient<RegistrarAlumnoViewModel>();
        services.AddTransient<EditarAlumnoViewModel>();

        services.AddScoped(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    #region ModalNavigationService
    private INavigationService CreateEditarAlumnoModalNavigationService(IServiceProvider serviceProvider)
    {
        return new ModalNavigationService<EditarAlumnoViewModel>(
            () => serviceProvider.GetRequiredService<EditarAlumnoViewModel>(),
            serviceProvider.GetRequiredService<ModalNavigationStore>());
    }
    #endregion

    #region NavigationServices
    private INavigationService CreateMainNavigationService(IServiceProvider serviceProvider)
    {
        return new MainNavigationService<MainViewModel>(
            () => serviceProvider.GetRequiredService<MainViewModel>(), serviceProvider.GetRequiredService<NavigationStore>());
    }

    private INavigationService CreateGestionAlumnosNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionAlumnosNavigationService<GestionAlumnosViewModel>(
            () => serviceProvider.GetRequiredService<GestionAlumnosViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    }

    private INavigationService CreateRegistrarAlumnoNavigationService(IServiceProvider serviceProvider)
    {
        return new RegistrarAlumnoNavigationService<RegistrarAlumnoViewModel>(
            () => serviceProvider.GetRequiredService<RegistrarAlumnoViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    }

    private INavigationService CreateVerPerfilNavigationService(IServiceProvider serviceProvider)
    {
        return new VerPerfilNavigationService<PerfilAlumnoViewModel>(
            () => serviceProvider.GetRequiredService<PerfilAlumnoViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    }
    #endregion

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
}