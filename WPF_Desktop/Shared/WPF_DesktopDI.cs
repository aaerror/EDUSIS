using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioAlumnos;
using Core.ServicioAutenticaciones;
using Core.ServicioCurriculas;
using Core.ServicioCursos;
using Core.ServicioDocentes;
using Core.ServicioDocumentos;
using Core.ServicioUsuarios;
using Microsoft.Extensions.DependencyInjection;
using System;
using WPF_Desktop.Navigation.NavigationServices.Cursos;
using WPF_Desktop.Navigation.NavigationServices.Docentes;
using WPF_Desktop.Navigation.NavigationServices.Modal;
using WPF_Desktop.Navigation.NavigationServices.Usuarios;
using WPF_Desktop.Navigation.NavigationServices;
using WPF_Desktop.Navigation;
using WPF_Desktop.Store.NavigationStore;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Alumnos;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;
using WPF_Desktop.ViewModels.Cursos.Curriculas;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Docentes.Licencias;
using WPF_Desktop.ViewModels.Docentes.Puestos;
using WPF_Desktop.ViewModels.Docentes;
using WPF_Desktop.ViewModels.Shared.Modal;
using WPF_Desktop.ViewModels.Usuarios;
using WPF_Desktop.ViewModels;
using WPF_Desktop.Navigation.NavigationServices.Alumnos;

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
		services.AddSingleton<MainWindowNavigationStore>();
		services.AddSingleton<StartupWindowNavigationStore>();
		services.AddSingleton<ModalWindowNavigationStore>();

		services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

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
		services.AddTransient<StartupViewModel>(provider =>
			new StartupViewModel(provider.GetRequiredService<StartupWindowNavigationStore>(),
								 CreateLoginUsuariosNavigationService(provider),
								 CreateRegistrarUsuariosNavigationService(provider)));

		services.AddTransient<MainViewModel>(provider =>
				new MainViewModel(provider.GetRequiredService<MainWindowNavigationStore>(),
								  provider.GetRequiredService<ModalWindowNavigationStore>(),
								  provider.GetRequiredService<IServicioUsuario>(),
								  provider.GetRequiredService<IServicioDocumento>(),
								  CreateGestionDocentesNavigationService(provider),
								  CreateGestionAlumnosNavigationService(provider),
								  CreateGestionCursosNavigationService(provider),
								  provider.GetRequiredService<IMessenger>()));

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
										CreateInscripcionAlumnoNavigationService(provider),
										CreateVerPerfilNavigationService(provider),
										provider.GetRequiredService<LegajoStore>()));

		services.AddTransient<PerfilAlumnoViewModel>(provider =>
			new PerfilAlumnoViewModel(provider.GetRequiredService<IServicioAlumno>(),
									  provider.GetRequiredService<LegajoStore>()));
		services.AddTransient<RegistrarAlumnoViewModel>();
		services.AddTransient<InscripcionAlumnoViewModel>();
		#endregion

		#region Cursos
		services.AddTransient<RegistrarCursosViewModel>(provider =>
			new RegistrarCursosViewModel(provider.GetRequiredService<IServicioCurso>(),
										 CreateGestionCursosNavigationService(provider)));
										 
		services.AddTransient<GestionCursantesViewModel>(provider =>
			new GestionCursantesViewModel(provider.GetRequiredService<IServicioCurso>(),
										  provider.GetRequiredService<IServicioCurricula>(),
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
										   CreateGestionCursosNavigationService(provider),
										   CreateGestionCursantesNavigationService(provider),
										   provider.GetRequiredService<CursoStore>(),
										   provider.GetRequiredService<DivisionStore>()));
		#endregion

		#region Materias
		services.AddTransient<GestionCurriculasViewModel>(provider =>
			new GestionCurriculasViewModel(CreateGestionCursosNavigationService(provider),
										 CreateGestionSituacionRevistaNavigationService(provider),
										 provider.GetRequiredService<IServicioCurricula>(),
										 provider.GetRequiredService<IServicioDocente>(),
										 provider.GetRequiredService<CursoStore>(),
										 provider.GetRequiredService<MateriaStore>()));
		#endregion

		#region SituaciónRevista
		services.AddTransient<GestionSituacionRevistaViewModel>(provider =>
			new GestionSituacionRevistaViewModel(CreateGestionDisenoCurricularNavigationService(provider),
												 provider.GetRequiredService<IServicioDocente>(),
												 provider.GetRequiredService<IServicioCurricula>(),
												 provider.GetRequiredService<CursoStore>(),
												 provider.GetRequiredService<MateriaStore>()));
		#endregion

		#region Usuarios
		services.AddTransient<RegistrarUsuarioViewModel>();

		services.AddTransient<LoginUsuarioViewModel>(provider =>
			new LoginUsuarioViewModel(CreateMainNavigationService(provider),
									  provider.GetRequiredService<IServicioAutenticacion>()));
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
			serviceProvider.GetRequiredService<ModalWindowNavigationStore>());
	#endregion

	#region MainNavigationServices
	private static INavigationService CreateMainNavigationService(IServiceProvider serviceProvider) =>
		new MainNavigationService<MainViewModel>(() =>
			serviceProvider.GetRequiredService<MainViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	#endregion

	#region DocentesNavigationServices
	private static INavigationService CreateGestionDocentesNavigationService(IServiceProvider serviceProvider) =>
		new GestionDocentesNavigationService<GestionDocentesViewModel>(() =>
			serviceProvider.GetRequiredService<GestionDocentesViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());

	private static INavigationService CreateRegistrarDocenteNavigationService(IServiceProvider serviceProvider) =>
		new RegistrarDocenteNavigationService<RegistrarDocenteViewModel>(() =>
			serviceProvider.GetRequiredService<RegistrarDocenteViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());

	private static INavigationService CreatePerfilDocenteNavigationService(IServiceProvider serviceProvider) =>
		new PerfilDocenteNavigationService<PerfilDocenteViewModel>(() =>
			serviceProvider.GetRequiredService<PerfilDocenteViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());

	// GESTIÓN PUESTOS
	private static INavigationService CreateGestionPuestosNavigationService(IServiceProvider serviceProvider) =>
		new GestionPuestosNavigationService<GestionPuestosViewModel>(() =>
			serviceProvider.GetRequiredService<GestionPuestosViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());

	// GESTIÓN LICENCIAS
	private static INavigationService CreateGestionLicenciasNavigationService(IServiceProvider serviceProvider) =>
		new GestionLicenciasNavigationService<GestionLicenciasViewModel>(() =>
			serviceProvider.GetRequiredService<GestionLicenciasViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	#endregion

	#region AlumnosNavigationServices
	private static INavigationService CreateGestionAlumnosNavigationService(IServiceProvider serviceProvider) =>
		new GestionAlumnosNavigationService<GestionAlumnosViewModel>(() =>
			serviceProvider.GetRequiredService<GestionAlumnosViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	private static INavigationService CreateRegistrarAlumnoNavigationService(IServiceProvider serviceProvider) =>
		new RegistrarAlumnoNavigationService<RegistrarAlumnoViewModel>(() =>
			serviceProvider.GetRequiredService<RegistrarAlumnoViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	private static INavigationService CreateInscripcionAlumnoNavigationService(IServiceProvider serviceProvider) =>
			new InscripcionAlumnoNavigationService<InscripcionAlumnoViewModel>(() =>
				serviceProvider.GetRequiredService<InscripcionAlumnoViewModel>(),
				serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	private static INavigationService CreateVerPerfilNavigationService(IServiceProvider serviceProvider) =>
		new VerPerfilNavigationService<PerfilAlumnoViewModel>(() =>
			serviceProvider.GetRequiredService<PerfilAlumnoViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	#endregion

	#region CursosNavigationServices
	private static INavigationService CreateGestionCursosNavigationService(IServiceProvider serviceProvider) =>
		new GestionCursosNavigationService<GestionCursosViewModel>(() =>
			serviceProvider.GetRequiredService<GestionCursosViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	private static INavigationService CreateRegistrarCursoNavigationService(IServiceProvider serviceProvider) =>
		new RegistrarCursoNavigationService<RegistrarCursosViewModel>(() =>
			serviceProvider.GetRequiredService<RegistrarCursosViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	private static INavigationService CreateGestionDivisionesNavigationService(IServiceProvider serviceProvider) =>
		new GestionDivisionesNavigationService<GestionDivisionesViewModel>(() =>
			serviceProvider.GetRequiredService<GestionDivisionesViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	private static INavigationService CreateGestionCursantesNavigationService(IServiceProvider serviceProvider) =>
		new GestionCursantesNavigationService<GestionCursantesViewModel>(() =>
			serviceProvider.GetRequiredService<GestionCursantesViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	#endregion

	#region MateriasNavigationServices
	private static INavigationService CreateGestionDisenoCurricularNavigationService(IServiceProvider serviceProvider) =>
		new GestionDisenoCurricularNavigationService<GestionCurriculasViewModel>(() =>
			serviceProvider.GetRequiredService<GestionCurriculasViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	#endregion

	#region SituaciónRevistaNavigationService
	private static INavigationService CreateGestionSituacionRevistaNavigationService(IServiceProvider serviceProvider) =>
		new GestionSituacionRevistaNavigationService<GestionSituacionRevistaViewModel>(() =>
			serviceProvider.GetRequiredService<GestionSituacionRevistaViewModel>(),
			serviceProvider.GetRequiredService<MainWindowNavigationStore>());
	#endregion

	#region UsuariosNavigationService
	private static INavigationService CreateLoginUsuariosNavigationService(IServiceProvider serviceProvider) =>
		new LoginUsuarioNavigationService<LoginUsuarioViewModel>(() =>
			serviceProvider.GetRequiredService<LoginUsuarioViewModel>(),
			serviceProvider.GetRequiredService<StartupWindowNavigationStore>());

	private static INavigationService CreateRegistrarUsuariosNavigationService(IServiceProvider serviceProvider) =>
		new RegistrarUsuarioNavigationService<RegistrarUsuarioViewModel>(() =>
			serviceProvider.GetRequiredService<RegistrarUsuarioViewModel>(),
			serviceProvider.GetRequiredService<StartupWindowNavigationStore>());
	#endregion
}