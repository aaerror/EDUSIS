using Core.ServicioAlumnos;
using Core.ServicioCursos;
using Core.ServicioDocentes;
using Core.ServicioMaterias;
using Core.Shared;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Docentes;
using WPF_Desktop.ViewModels.Materias;

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
        /*services.AddScoped<IMediator, Mediator>();
        services.AddScoped<IUnitOfWork>(provider => new UnitOfWork(provider.GetRequiredService<IMediator>()));*/
        InfrastructureDI.AddInfrastructure(services);
        #endregion

        #region Core
        CoreDI.AddServices(services);
        /*services.AddTransient<IServicioAlumnos, ServicioAlumnos>();
        services.AddTransient<IServicioDocentes, ServicioDocentes>();
        services.AddTransient<IServicioCursos, ServicioCursos>();
        services.AddTransient<IServicioMaterias, ServicioMaterias>();
        services.AddTransient<IServicioAutenticacion, ServicioAutenticacion>();*/
        #endregion

        /**
         * UI
         */
        // Stores
        services.AddSingleton<ModalNavigationStore>();
        services.AddSingleton<NavigationStore>();
        services.AddSingleton<PerfilBuscadoStore>();
        services.AddSingleton<CursoStore>();
        services.AddSingleton<DivisionStore>();

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
        services.AddTransient<GestionDocentesViewModel>(provider => new GestionDocentesViewModel(provider.GetRequiredService<IServicioDocente>(),
                                                                                                 CreateRegistrarDocenteNavigationService(provider),
                                                                                                 CreatePerfilDocenteNavigationService(provider),
                                                                                                 CreateGestionPuestosNavigationService(provider),
                                                                                                 CreateGestionLicenciasNavigationService(provider),
                                                                                                 provider.GetRequiredService<PerfilBuscadoStore>()));
        services.AddTransient<RegistrarDocenteViewModel>(provider => new RegistrarDocenteViewModel(provider.GetRequiredService<IServicioDocente>()));
        services.AddTransient<PerfilDocenteViewModel>(provider => new PerfilDocenteViewModel(provider.GetRequiredService<IServicioDocente>(),
                                                                                             provider.GetRequiredService<PerfilBuscadoStore>()));
        services.AddTransient<GestionPuestosViewModel>();
        services.AddTransient<GestionLicenciasViewModel>();

        // ALUMNOS
        services.AddTransient<GestionAlumnosViewModel>(provider => new GestionAlumnosViewModel(provider.GetRequiredService<IServicioAlumno>(),
                                                                                               CreateRegistrarAlumnoNavigationService(provider),
                                                                                               CreateVerPerfilNavigationService(provider),
                                                                                               provider.GetRequiredService<PerfilBuscadoStore>()));

        services.AddTransient<PerfilAlumnoViewModel>(provider => new PerfilAlumnoViewModel(provider.GetRequiredService<IServicioAlumno>(),
                                                                                           provider.GetRequiredService<PerfilBuscadoStore>()));
        services.AddTransient<RegistrarAlumnoViewModel>();

        #region Cursos
        services.AddTransient<RegistrarCursosViewModel>();
        services.AddTransient<GestionCursantesViewModel>();
        services.AddTransient<GestionCursosViewModel>(provider => new GestionCursosViewModel(provider.GetRequiredService<IServicioCurso>(),
                                                                                             CreateRegistrarCursoNavigationService(provider),
                                                                                             CreateGestionDivisionesNavigationService(provider),
                                                                                             CreateGestionDisenoCurricularNavigationService(provider),
                                                                                             provider.GetRequiredService<CursoStore>()));
        services.AddTransient<GestionDivisionesViewModel>(provider => new GestionDivisionesViewModel(provider.GetRequiredService<IServicioCurso>(),
                                                                                                     provider.GetRequiredService<IServicioDocente>(),
                                                                                                     CreateGestionCursantesNavigationService(provider),
                                                                                                     provider.GetRequiredService<CursoStore>(),
                                                                                                     provider.GetRequiredService<DivisionStore>()));
        #endregion

        #region Materias
        services.AddTransient<GestionMateriasViewModel>(provider => new GestionMateriasViewModel(provider.GetRequiredService<IServicioMateria>(),
                                                                                                 provider.GetRequiredService<IServicioDocente>(),
                                                                                                 provider.GetRequiredService<CursoStore>()));
        services.AddTransient<MateriaViewModel>();
        #endregion

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

    #region Docentes
    // GESTIÓN DOCENTES
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
    // GESTIÓN PUESTOS
    private INavigationService CreateGestionPuestosNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionPuestosNavigationService<GestionPuestosViewModel>(() => serviceProvider.GetRequiredService<GestionPuestosViewModel>(),
                                                                            serviceProvider.GetRequiredService<NavigationStore>());
    }
    // GESTIÓN LICENCIAS
    private INavigationService CreateGestionLicenciasNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionLicenciasNavigationService<GestionLicenciasViewModel>(() => serviceProvider.GetRequiredService<GestionLicenciasViewModel>(),
                                                                                serviceProvider.GetRequiredService<NavigationStore>());
    }
    #endregion

    #region Alumnos
    // GESTIÓN ALUMNOS
    private INavigationService CreateGestionAlumnosNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionAlumnosNavigationService<GestionAlumnosViewModel>(() => serviceProvider.GetRequiredService<GestionAlumnosViewModel>(),
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

    #region Cursos
    // GESTIÓN CURSOS
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
    // GESTIÓN DIVISIONES
    private INavigationService CreateGestionDivisionesNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionDivisionesNavigationService<GestionDivisionesViewModel>(() => serviceProvider.GetRequiredService<GestionDivisionesViewModel>(),
                                                                                  serviceProvider.GetRequiredService<NavigationStore>());
    }

    private INavigationService CreateGestionCursantesNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionCursantesNavigationService<GestionCursantesViewModel>(() => serviceProvider.GetRequiredService<GestionCursantesViewModel>(),
                                                                                serviceProvider.GetRequiredService<NavigationStore>());
    }
    #endregion

    #region Materias
    // GESTIÓN DISENO CURRICULAR
    private INavigationService CreateGestionDisenoCurricularNavigationService(IServiceProvider serviceProvider)
    {
        return new GestionDisenoCurricularNavigationService<GestionMateriasViewModel>(() => serviceProvider.GetRequiredService<GestionMateriasViewModel>(),
                                                                                            serviceProvider.GetRequiredService<NavigationStore>());
    }
    #endregion
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

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        string caption = "Ha ocurrido un error inesperado";
        string message = e.Exception.Message;
        MessageBox.Show($"Error: { message }", caption, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
        e.Handled = true;
    }
}