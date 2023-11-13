using Core.ServicioAlumnos;
using Core.ServicioCursos;
using Core.ServicioDocentes;
using Infrastructure;
using Infrastructure.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
using WPF_Desktop.Navigation;
using WPF_Desktop.Navigation.NavigationServices;
using WPF_Desktop.Navigation.NavigationServices.Cursos;
using WPF_Desktop.Navigation.NavigationServices.Docentes;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels;
using WPF_Desktop.ViewModels.Alumnos;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Docentes;

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
        services.AddSingleton<IServicioAlumnos, ServicioAlumnos>();
        services.AddSingleton<IServicioDocentes, ServicioDocentes>();
        services.AddSingleton<IServicioCursos, ServicioCursos>();

        /**
         * UI
         */
        // Stores
        services.AddSingleton<ModalNavigationStore>();
        services.AddSingleton<NavigationStore>();
        services.AddSingleton<PerfilBuscadoStore>();
        services.AddSingleton<CursoStore>();
        // Navigation
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INavigationService>(provider => CreateMainNavigationService(provider));

        /**
         * VIEW MODELS
         */
        services.AddTransient<MainViewModel>(provider => new MainViewModel(provider.GetRequiredService<ModalNavigationStore>(),
                                                                           provider.GetRequiredService<NavigationStore>(),
                                                                           CreateGestionDocentesNavigationService(provider),
                                                                           CreateGestionAlumnosNavigationService(provider),
                                                                           CreateGestionCursosNavigationService(provider)));
        
        services.AddTransient<LoginViewModel>();

        // DOCENTES
        services.AddTransient<GestionDocentesViewModel>(provider => new GestionDocentesViewModel(provider.GetRequiredService<IServicioDocentes>(),
                                                                                                 CreateRegistrarDocenteNavigationService(provider),
                                                                                                 CreatePerfilDocenteNavigationService(provider),
                                                                                                 CreateGestionPuestosNavigationService(provider),
                                                                                                 CreateGestionLicenciasNavigationService(provider),
                                                                                                 provider.GetRequiredService<PerfilBuscadoStore>()));
        services.AddTransient<RegistrarDocenteViewModel>(provider => new RegistrarDocenteViewModel(provider.GetRequiredService<IServicioDocentes>()));
        services.AddTransient<PerfilDocenteViewModel>(provider => new PerfilDocenteViewModel(provider.GetRequiredService<IServicioDocentes>(),
                                                                                             provider.GetRequiredService<PerfilBuscadoStore>()));
        services.AddTransient<GestionPuestosViewModel>();
        services.AddTransient<GestionLicenciasViewModel>();

        // ALUMNOS
        services.AddTransient<GestionAlumnosViewModel>(provider => new GestionAlumnosViewModel(provider.GetRequiredService<IServicioAlumnos>(),
                                                                                               CreateRegistrarAlumnoNavigationService(provider),
                                                                                               CreateVerPerfilNavigationService(provider),
                                                                                               provider.GetRequiredService<PerfilBuscadoStore>()));

        services.AddTransient<PerfilAlumnoViewModel>(provider => new PerfilAlumnoViewModel(provider.GetRequiredService<IServicioAlumnos>(),
                                                                                           provider.GetRequiredService<PerfilBuscadoStore>()));
        services.AddTransient<RegistrarAlumnoViewModel>();

        // CURSOS
        services.AddTransient<GestionCursosViewModel>(provider => new GestionCursosViewModel(provider.GetRequiredService<IServicioCursos>(),
                                                                                             CreateRegistrarCursoNavigationService(provider),
                                                                                             CreateGestionDisenoCurricularNavigationService(provider),
                                                                                             provider.GetRequiredService<CursoStore>()));
        services.AddTransient<RegistrarCursosViewModel>();
        services.AddTransient<GestionDisenoCurriculaViewModel>(provider => new GestionDisenoCurriculaViewModel(provider.GetRequiredService<IServicioCursos>(),
                                                                                                               provider.GetRequiredService<IServicioDocentes>(),
                                                                                                               provider.GetRequiredService<CursoStore>()));
        services.AddTransient<MateriaDetalleViewModel>();

        services.AddScoped(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    #region ModalNavigationService
/*    private INavigationService CreateEditarAlumnoModalNavigationService(IServiceProvider serviceProvider)
    {
        return new ModalNavigationService<EditarAlumnoViewModel>(() => serviceProvider.GetRequiredService<EditarAlumnoViewModel>(),
                                                                 serviceProvider.GetRequiredService<ModalNavigationStore>());
    }*/
    #endregion

    #region NavigationServices
    // GESTION MAIN
    private INavigationService CreateMainNavigationService(IServiceProvider serviceProvider)
    {
        return new MainNavigationService<MainViewModel>(() => serviceProvider.GetRequiredService<MainViewModel>(),
                                                        serviceProvider.GetRequiredService<NavigationStore>());
    }

    // GESTION DOCENTES
    private INavigationService CreateGestionDocentesNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionDocentesNavigationService<GestionDocentesViewModel>(() => serviceProvider.GetRequiredService<GestionDocentesViewModel>(),
                                                                              serviceProvider.GetRequiredService<NavigationStore>());
    }

    private INavigationService CreateRegistrarDocenteNavigationService(IServiceProvider serviceProvider)
    {
        return new RegistrarDocenteNavigationService<RegistrarDocenteViewModel>(() => serviceProvider.GetRequiredService<RegistrarDocenteViewModel>(),
                                                                                serviceProvider.GetRequiredService<NavigationStore>());
    }

    private INavigationService CreatePerfilDocenteNavigationService(IServiceProvider serviceProvider)
    {
        return new PerfilDocenteNavigationService<PerfilDocenteViewModel>(() => serviceProvider.GetRequiredService<PerfilDocenteViewModel>(),
                                                                          serviceProvider.GetRequiredService<NavigationStore>());
    }

    private INavigationService CreateGestionPuestosNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionPuestosNavigationService<GestionPuestosViewModel>(() => serviceProvider.GetRequiredService<GestionPuestosViewModel>(),
                                                                            serviceProvider.GetRequiredService<NavigationStore>());
    }

    private INavigationService CreateGestionLicenciasNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionLicenciasNavigationService<GestionLicenciasViewModel>(() => serviceProvider.GetRequiredService<GestionLicenciasViewModel>(),
                                                                                serviceProvider.GetRequiredService<NavigationStore>());
    }

    // GESTION ALUMNOS
    private INavigationService CreateGestionAlumnosNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionAlumnosNavigationService<GestionAlumnosViewModel>(() => serviceProvider.GetRequiredService<GestionAlumnosViewModel>(),
                                                                            serviceProvider.GetRequiredService<NavigationStore>());
    }

    // GESTION CURSOS
    private INavigationService CreateGestionCursosNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionCursosNavigationService<GestionCursosViewModel>(() => serviceProvider.GetRequiredService<GestionCursosViewModel>(),
                                                                          serviceProvider.GetRequiredService<NavigationStore>());
    }

    // REGISTRAR CURSO
    private INavigationService CreateRegistrarCursoNavigationService(IServiceProvider serviceProvider)
    {
        return new RegistrarCursoNavigationService<RegistrarCursosViewModel>(() => serviceProvider.GetRequiredService<RegistrarCursosViewModel>(),
                                                                             serviceProvider.GetRequiredService<NavigationStore>());
    }

    // GESITON DISENO CURRICULAR
    private INavigationService CreateGestionDisenoCurricularNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionDisenoCurricularNavigationService<GestionDisenoCurriculaViewModel>(() => serviceProvider.GetRequiredService<GestionDisenoCurriculaViewModel>(),
                                                                                             serviceProvider.GetRequiredService<NavigationStore>());
    }

    // REGISTRAR ALUMNO
    private INavigationService CreateRegistrarAlumnoNavigationService(IServiceProvider serviceProvider)
    {
        return new RegistrarAlumnoNavigationService<RegistrarAlumnoViewModel>(() => serviceProvider.GetRequiredService<RegistrarAlumnoViewModel>(),
                                                                              serviceProvider.GetRequiredService<NavigationStore>());
    }

    // VER PERFIL
    private INavigationService CreateVerPerfilNavigationService(IServiceProvider serviceProvider)
    {
        return new VerPerfilNavigationService<PerfilAlumnoViewModel>(() => serviceProvider.GetRequiredService<PerfilAlumnoViewModel>(),
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