using Core.ServicioDocentes;
using Microsoft.Extensions.DependencyInjection;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Navigation.NavigationServices;
using WPF_Desktop.Navigation.NavigationServices.Cursos;
using WPF_Desktop.Navigation.NavigationServices.Docentes;
using WPF_Desktop.Navigation.NavigationServices.Modal;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels;
using WPF_Desktop.ViewModels.Alumnos;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Docentes;
using Core.ServicioAlumnos;
using Core.ServicioCursos;
using Core.ServicioMaterias;
using WPF_Desktop.ViewModels.Shared.Modal;
using WPF_Desktop.Store.Modal;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;


namespace WPF_Desktop.Shared;

internal static class WPF_DesktopDI
{
    public static IServiceCollection AddWPFDesktopDI(this IServiceCollection services)
    {
        AddStores(services);
        AddViewModels(services);
        AddNavigationServices(services);

        return services;
    }

    #region Stores
    private static IServiceCollection AddStores(this IServiceCollection services)
    {
        services.AddSingleton<ModalNavigationStore>();
        services.AddSingleton<NavigationStore>();
        services.AddSingleton<LegajoStore>();
        services.AddSingleton<CursoStore>();
        services.AddSingleton<DivisionStore>();
        services.AddSingleton<MateriaStore>();

        return services;
    }
    #endregion

    #region ViewModels
    private static IServiceCollection AddViewModels(this IServiceCollection services)
    {

        services.AddTransient<LoginViewModel>();

        services.AddTransient<MainViewModel>(provider =>
                new MainViewModel(provider.GetRequiredService<ModalNavigationStore>(),
                                  provider.GetRequiredService<NavigationStore>(),
                                  CreateGestionDocentesNavigationService(provider),
                                  CreateGestionAlumnosNavigationService(provider),
                                  CreateGestionCursosNavigationService(provider)));

        services.AddTransient<BuscarViewModel>();

        #region Docentes
        services.AddTransient<GestionDocentesViewModel>(provider =>
            new GestionDocentesViewModel(provider.GetRequiredService<IServicioDocente>(),
                                         CreateRegistrarDocenteNavigationService(provider),
                                         CreatePerfilDocenteNavigationService(provider),
                                         CreateGestionPuestosNavigationService(provider),
                                         CreateGestionLicenciasNavigationService(provider),
                                         provider.GetRequiredService<LegajoStore>()));
        services.AddTransient<RegistrarDocenteViewModel>(provider =>
            new RegistrarDocenteViewModel(provider.GetRequiredService<IServicioDocente>()));
        services.AddTransient<PerfilDocenteViewModel>(provider =>
            new PerfilDocenteViewModel(provider.GetRequiredService<IServicioDocente>(),
                                       provider.GetRequiredService<LegajoStore>()));
        services.AddTransient<GestionPuestosViewModel>();
        services.AddTransient<GestionLicenciasViewModel>();
        #endregion

        #region Alumnos
        services.AddTransient<GestionAlumnosViewModel>(provider =>
            new GestionAlumnosViewModel(provider.GetRequiredService<IServicioAlumno>(),
                                        CreateRegistrarAlumnoNavigationService(provider),
                                        CreateVerPerfilNavigationService(provider),
                                        provider.GetRequiredService<LegajoStore>()));

        services.AddTransient<PerfilAlumnoViewModel>(provider =>
            new PerfilAlumnoViewModel(provider.GetRequiredService<IServicioAlumno>(),
                                      provider.GetRequiredService<LegajoStore>()));
        services.AddTransient<RegistrarAlumnoViewModel>();
        #endregion
        
        #region Cursos
        services.AddTransient<RegistrarCursosViewModel>();
        services.AddTransient<GestionCursantesViewModel>(provider =>
            new GestionCursantesViewModel(provider.GetRequiredService<ServicioCurso>(),
                                          provider.GetRequiredService<ServicioMateria>(),
                                          provider.GetRequiredService<CursoStore>(),
                                          provider.GetRequiredService<DivisionStore>()));
        services.AddTransient<GestionCursosViewModel>(provider =>
            new GestionCursosViewModel(provider.GetRequiredService<IServicioCurso>(),
                                       CreateRegistrarCursoNavigationService(provider),
                                       CreateGestionDivisionesNavigationService(provider),
                                       CreateGestionDisenoCurricularNavigationService(provider),
                                       provider.GetRequiredService<CursoStore>()));
        services.AddTransient<GestionDivisionesViewModel>(provider =>
            new GestionDivisionesViewModel(provider.GetRequiredService<IServicioCurso>(),
                                           provider.GetRequiredService<IServicioDocente>(),
                                           CreateGestionCursantesNavigationService(provider),
                                           provider.GetRequiredService<CursoStore>(),
                                           provider.GetRequiredService<DivisionStore>()));
        #endregion

        #region Materias
        services.AddTransient<GestionMateriasViewModel>(provider =>
            new GestionMateriasViewModel(CreateGestionSituacionRevistaNavigationService(provider),
                                         provider.GetRequiredService<IServicioMateria>(),
                                         provider.GetRequiredService<IServicioDocente>(),
                                         provider.GetRequiredService<CursoStore>(),
                                         provider.GetRequiredService<MateriaStore>()));
        #endregion

        #region SituaciónRevista
        services.AddTransient<GestionSituacionRevistaViewModel>(provider =>
            new GestionSituacionRevistaViewModel(provider.GetRequiredService<IServicioDocente>(),
                                                 provider.GetRequiredService<IServicioMateria>(),
                                                 provider.GetRequiredService<CursoStore>(),
                                                 provider.GetRequiredService<MateriaStore>()));
        #endregion

        return services;
    }
    #endregion

    #region NavigationServices
    private static IServiceCollection AddNavigationServices(IServiceCollection services)
    {
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INavigationService>(provider =>
            CreateMainNavigationService(provider));

        return services;
    }
    #endregion

    #region ModalNavigationService
    private static INavigationService CreateBuscarModalNavigationService(IServiceProvider serviceProvider) =>
        new ModalNavigationService<BuscarViewModel>(() =>
            serviceProvider.GetRequiredService<BuscarViewModel>(),
            serviceProvider.GetRequiredService<ModalNavigationStore>());
    #endregion

    #region MainNavigationServices
    private static INavigationService CreateMainNavigationService(IServiceProvider serviceProvider) =>
        new MainNavigationService<MainViewModel>(() =>
            serviceProvider.GetRequiredService<MainViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    #endregion

    #region DocentesNavigationServices
    private static INavigationService CreateGestionDocentesNavigationService(IServiceProvider serviceProvider) =>
        new GestionDocentesNavigationService<GestionDocentesViewModel>(() =>
            serviceProvider.GetRequiredService<GestionDocentesViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());

    private static INavigationService CreateRegistrarDocenteNavigationService(IServiceProvider serviceProvider) =>
        new RegistrarDocenteNavigationService<RegistrarDocenteViewModel>(() =>
            serviceProvider.GetRequiredService<RegistrarDocenteViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());

    private static INavigationService CreatePerfilDocenteNavigationService(IServiceProvider serviceProvider) =>
        new PerfilDocenteNavigationService<PerfilDocenteViewModel>(() =>
            serviceProvider.GetRequiredService<PerfilDocenteViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());

    // GESTIÓN PUESTOS
    private static INavigationService CreateGestionPuestosNavigationService(IServiceProvider serviceProvider) =>
        new GestionPuestosNavigationService<GestionPuestosViewModel>(() =>
            serviceProvider.GetRequiredService<GestionPuestosViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());

    // GESTIÓN LICENCIAS
    private static INavigationService CreateGestionLicenciasNavigationService(IServiceProvider serviceProvider) =>
        new GestionLicenciasNavigationService<GestionLicenciasViewModel>(() =>
            serviceProvider.GetRequiredService<GestionLicenciasViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    #endregion

    #region AlumnosNavigationServices
    private static INavigationService CreateGestionAlumnosNavigationService(IServiceProvider serviceProvider) =>
        new GestionAlumnosNavigationService<GestionAlumnosViewModel>(() =>
            serviceProvider.GetRequiredService<GestionAlumnosViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    private static INavigationService CreateRegistrarAlumnoNavigationService(IServiceProvider serviceProvider) =>
        new RegistrarAlumnoNavigationService<RegistrarAlumnoViewModel>(() =>
            serviceProvider.GetRequiredService<RegistrarAlumnoViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    private static INavigationService CreateVerPerfilNavigationService(IServiceProvider serviceProvider) =>
        new VerPerfilNavigationService<PerfilAlumnoViewModel>(() =>
            serviceProvider.GetRequiredService<PerfilAlumnoViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    #endregion

    #region CursosNavigationServices
    private static INavigationService CreateGestionCursosNavigationService(IServiceProvider serviceProvider) =>
        new GestionCursosNavigationService<GestionCursosViewModel>(() =>
            serviceProvider.GetRequiredService<GestionCursosViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    private static INavigationService CreateRegistrarCursoNavigationService(IServiceProvider serviceProvider) =>
        new RegistrarCursoNavigationService<RegistrarCursosViewModel>(() =>
            serviceProvider.GetRequiredService<RegistrarCursosViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    private static INavigationService CreateGestionDivisionesNavigationService(IServiceProvider serviceProvider) =>
        new GestionDivisionesNavigationService<GestionDivisionesViewModel>(() =>
            serviceProvider.GetRequiredService<GestionDivisionesViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    private static INavigationService CreateGestionCursantesNavigationService(IServiceProvider serviceProvider) =>
        new GestionCursantesNavigationService<GestionCursantesViewModel>(() =>
            serviceProvider.GetRequiredService<GestionCursantesViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    #endregion

    #region MateriasNavigationServices
    private static INavigationService CreateGestionDisenoCurricularNavigationService(IServiceProvider serviceProvider) =>
        new GestionDisenoCurricularNavigationService<GestionMateriasViewModel>(() =>
            serviceProvider.GetRequiredService<GestionMateriasViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    #endregion

    #region SituaciónRevistaNavigationService
    private static INavigationService CreateGestionSituacionRevistaNavigationService(IServiceProvider serviceProvider) =>
        new GestionSituacionRevistaNavigationService<GestionSituacionRevistaViewModel>(() =>
            serviceProvider.GetRequiredService<GestionSituacionRevistaViewModel>(),
            serviceProvider.GetRequiredService<NavigationStore>());
    #endregion
}
