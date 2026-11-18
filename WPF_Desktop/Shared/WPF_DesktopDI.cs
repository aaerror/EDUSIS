using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioAlumnos;
using Core.ServicioAsistencias;
using Core.ServicioAutenticaciones;
using Core.ServicioCatedras;
using Core.ServicioCurriculas;
using Core.ServicioCursos;
using Core.ServicioDivisiones;
using Core.ServicioDocentes;
using Core.ServicioLicencias;
using Core.ServicioCursantes;
using Core.ServicioDocumentos;
using Core.ServicioMaterias;
using Core.ServicioReportes;
using Core.ServicioUsuarios;
using Core.Shared.Seguridad;
using Microsoft.Extensions.DependencyInjection;
using System;
using WPF_Desktop.Security;
using WPF_Desktop.Navigation;
using WPF_Desktop.Store.NavigationStore;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Alumnos;
using WPF_Desktop.ViewModels.Asistencias;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.Catedras;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;
using WPF_Desktop.ViewModels.Cursos.Curriculas;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Documentacion;
using WPF_Desktop.ViewModels.Reportes;
using WPF_Desktop.ViewModels.Docentes.Licencias;
using WPF_Desktop.ViewModels.Docentes.Puestos;
using WPF_Desktop.ViewModels.Docentes;
using WPF_Desktop.ViewModels.Shared.Modal;
using WPF_Desktop.ViewModels.Usuarios;
using WPF_Desktop.ViewModels;

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
		services.AddSingleton<CicloLectivoStore>();
		services.AddSingleton<CatedraStore>();
		// Parámetro de navegación del modal: PARA QUÉ se lo abre. Ver BuscarContexto.
		services.AddSingleton<BuscarContextoStore>();

		// La cola de avisos al pie es estado compartido por toda la ventana, igual que los
		// stores: la escribe el DialogService desde cualquier pantalla y la dibuja el shell.
		services.AddSingleton<AvisoStore>();

		// La sesión es estado compartido entre pantallas, como los stores, y Singleton porque hay
		// un único usuario logueado por proceso.
		//
		// OJO con las tres líneas: se registra la clase concreta y las dos interfaces resuelven
		// HACIA ELLA. Registrar `AddSingleton<ISesionUsuario, SesionUsuario>()` y
		// `AddSingleton<ISesionActual, SesionUsuario>()` por separado crearía DOS instancias, y
		// entonces el login escribiría en una y los casos de uso de Core leerían la otra: la
		// autorización negaría todo sin un solo error.
		services.AddSingleton<SesionUsuario>();
		services.AddSingleton<ISesionUsuario>(provider => provider.GetRequiredService<SesionUsuario>());
		services.AddSingleton<ISesionActual>(provider => provider.GetRequiredService<SesionUsuario>());

		AddEstadoDeSesion(services);

		return services;
	}

	/// <summary>
	/// Segundo registro de cada store que guarda estado del USUARIO, para que el ciclo de sesión de
	/// <c>App</c> pueda barrerlos todos sin nombrar a ninguno al cambiar de usuario.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Mismo patrón —y misma trampa— que las tres líneas de la sesión de arriba: se resuelve HACIA la
	/// instancia ya registrada con <c>GetRequiredService</c>, nunca
	/// <c>AddSingleton&lt;IEstadoDeSesion, CursoStore&gt;()</c>. Esa forma crearía un
	/// <c>CursoStore</c> aparte: las pantallas escribirían en uno y el barrido limpiaría el otro, y
	/// el usuario siguiente heredaría el curso del anterior sin un solo error.
	/// </para>
	/// <para>
	/// Un store nuevo que guarde estado del usuario agrega su línea acá. Si se olvida, lo que
	/// guardó sobrevive al cambio de usuario en silencio: por eso existe la guarda estática de
	/// <c>tests/WPF_Desktop.UnitTests</c> que exige que todo tipo <c>*Store</c> de la capa
	/// implemente <c>IEstadoDeSesion</c> y esté en este método.
	/// </para>
	/// </remarks>
	private static IServiceCollection AddEstadoDeSesion(this IServiceCollection services)
	{
		services.AddSingleton<IEstadoDeSesion>(provider => provider.GetRequiredService<LegajoStore>());
		services.AddSingleton<IEstadoDeSesion>(provider => provider.GetRequiredService<CursoStore>());
		services.AddSingleton<IEstadoDeSesion>(provider => provider.GetRequiredService<DivisionStore>());
		services.AddSingleton<IEstadoDeSesion>(provider => provider.GetRequiredService<MateriaStore>());
		services.AddSingleton<IEstadoDeSesion>(provider => provider.GetRequiredService<CicloLectivoStore>());
		services.AddSingleton<IEstadoDeSesion>(provider => provider.GetRequiredService<CatedraStore>());
		services.AddSingleton<IEstadoDeSesion>(provider => provider.GetRequiredService<BuscarContextoStore>());
		services.AddSingleton<IEstadoDeSesion>(provider => provider.GetRequiredService<AvisoStore>());

		return services;
	}
	#endregion

	#region ViewModels
	private static IServiceCollection AddViewModels(this IServiceCollection services)
	{
		services.AddTransient<StartupViewModel>(provider =>
			new StartupViewModel(provider.GetRequiredService<StartupWindowNavigationStore>(),
								 Navegacion<LoginUsuarioViewModel, StartupWindowNavigationStore>(provider),
								 Navegacion<RegistrarUsuarioViewModel, StartupWindowNavigationStore>(provider)));

		services.AddTransient<MainViewModel>(provider =>
				new MainViewModel(provider.GetRequiredService<MainWindowNavigationStore>(),
								  provider.GetRequiredService<ModalWindowNavigationStore>(),
								  provider.GetRequiredService<IServicioUsuario>(),
								  Navegacion<GestionDocentesViewModel, MainWindowNavigationStore>(provider),
								  Navegacion<GestionAlumnosViewModel, MainWindowNavigationStore>(provider),
								  Navegacion<GestionCursosViewModel, MainWindowNavigationStore>(provider),
								  Navegacion<AsistenciasViewModel, MainWindowNavigationStore>(provider),
								  Navegacion<GestionUsuariosViewModel, MainWindowNavigationStore>(provider),
								  Navegacion<GestionDocumentacionViewModel, MainWindowNavigationStore>(provider),
								  Navegacion<GestionReportesViewModel, MainWindowNavigationStore>(provider),
								  provider.GetRequiredService<ISesionUsuario>(),
								  provider.GetRequiredService<IDialogService>(),
								  provider.GetRequiredService<AvisoStore>(),
								  provider.GetRequiredService<IMessenger>()));

		services.AddTransient<GestionUsuariosViewModel>(provider =>
			new GestionUsuariosViewModel(provider.GetRequiredService<IServicioUsuario>(),
										 provider.GetRequiredService<IServicioDocente>(),
										 provider.GetRequiredService<IDialogService>(), provider.GetRequiredService<ISesionActual>()));

	services.AddTransient<BuscarViewModel>(provider =>
			new BuscarViewModel(provider.GetRequiredService<IServicioDocente>(),
								provider.GetRequiredService<IDialogService>(),
								provider.GetRequiredService<ModalWindowNavigationStore>(),
								provider.GetRequiredService<BuscarContextoStore>(),
								provider.GetRequiredService<IMessenger>()));

		#region Docentes
		services.AddTransient<GestionDocentesViewModel>(provider =>
			new GestionDocentesViewModel(provider.GetRequiredService<IServicioDocente>(),
										 provider.GetRequiredService<IDialogService>(),
										 Navegacion<RegistrarDocenteViewModel, MainWindowNavigationStore>(provider),
										 Navegacion<PerfilDocenteViewModel, MainWindowNavigationStore>(provider),
										 Navegacion<GestionPuestosViewModel, MainWindowNavigationStore>(provider),
										 Navegacion<GestionLicenciasViewModel, MainWindowNavigationStore>(provider),
										 provider.GetRequiredService<LegajoStore>(), provider.GetRequiredService<ISesionActual>()));
		services.AddTransient<RegistrarDocenteViewModel>(provider =>
			new RegistrarDocenteViewModel(provider.GetRequiredService<IServicioDocente>(),
										  provider.GetRequiredService<IDialogService>(),
										  Navegacion<GestionDocentesViewModel, MainWindowNavigationStore>(provider), provider.GetRequiredService<ISesionActual>()));
		services.AddTransient<PerfilDocenteViewModel>(provider =>
			new PerfilDocenteViewModel(provider.GetRequiredService<IServicioDocente>(),
									   provider.GetRequiredService<IDialogService>(),
									   Navegacion<GestionDocentesViewModel, MainWindowNavigationStore>(provider),
									   provider.GetRequiredService<LegajoStore>()));
		// Las dos se registran con fábrica y no por reflexión: reciben un
		// INavigationService<GestionDocentesViewModel>, que no está registrado en el contenedor —cada
		// destino se arma con Navegacion<,>—, así que la resolución automática fallaría al navegar.
		services.AddTransient<GestionPuestosViewModel>(provider =>
			new GestionPuestosViewModel(provider.GetRequiredService<IServicioDocente>(),
										provider.GetRequiredService<IDialogService>(),
										Navegacion<GestionDocentesViewModel, MainWindowNavigationStore>(provider),
										provider.GetRequiredService<LegajoStore>(), provider.GetRequiredService<ISesionActual>()));
		services.AddTransient<GestionLicenciasViewModel>(provider =>
			new GestionLicenciasViewModel(provider.GetRequiredService<IServicioLicencia>(),
										  provider.GetRequiredService<IDialogService>(),
										  Navegacion<GestionDocentesViewModel, MainWindowNavigationStore>(provider),
										  provider.GetRequiredService<LegajoStore>()));
		#endregion

		#region Alumnos
		services.AddTransient<GestionAlumnosViewModel>(provider =>
			new GestionAlumnosViewModel(provider.GetRequiredService<IServicioAlumno>(),
										provider.GetRequiredService<IDialogService>(),
										Navegacion<RegistrarAlumnoViewModel, MainWindowNavigationStore>(provider),
										Navegacion<InscripcionAlumnoViewModel, MainWindowNavigationStore>(provider),
										Navegacion<PerfilAlumnoViewModel, MainWindowNavigationStore>(provider),
										provider.GetRequiredService<LegajoStore>(), provider.GetRequiredService<ISesionActual>()));

		services.AddTransient<PerfilAlumnoViewModel>(provider =>
			new PerfilAlumnoViewModel(provider.GetRequiredService<IServicioAlumno>(),
									  provider.GetRequiredService<IDialogService>(),
									  Navegacion<GestionAlumnosViewModel, MainWindowNavigationStore>(provider),
									  provider.GetRequiredService<LegajoStore>()));
		services.AddTransient<RegistrarAlumnoViewModel>(provider =>
			new RegistrarAlumnoViewModel(provider.GetRequiredService<IServicioAlumno>(),
										 provider.GetRequiredService<IServicioCurso>(),
										 provider.GetRequiredService<IServicioDivision>(),
										 provider.GetRequiredService<IDialogService>(),
										 Navegacion<GestionAlumnosViewModel, MainWindowNavigationStore>(provider),
										 provider.GetRequiredService<CicloLectivoStore>(), provider.GetRequiredService<ISesionActual>()));
		services.AddTransient<InscripcionAlumnoViewModel>(provider =>
			new InscripcionAlumnoViewModel(provider.GetRequiredService<IServicioCurso>(),
										   provider.GetRequiredService<IServicioCursante>(),
										   provider.GetRequiredService<IServicioDivision>(),
										   provider.GetRequiredService<IDialogService>(),
										   Navegacion<GestionAlumnosViewModel, MainWindowNavigationStore>(provider),
										   provider.GetRequiredService<LegajoStore>(),
										   provider.GetRequiredService<CicloLectivoStore>(), provider.GetRequiredService<ISesionActual>()));
		#endregion

		#region Cursos
		services.AddTransient<RegistrarCursosViewModel>(provider =>
			new RegistrarCursosViewModel(provider.GetRequiredService<IServicioCurso>(),
										 provider.GetRequiredService<IDialogService>(),
										 Navegacion<GestionCursosViewModel, MainWindowNavigationStore>(provider)));
										 
		services.AddTransient<GestionCursantesViewModel>(provider =>
			new GestionCursantesViewModel(provider.GetRequiredService<IServicioCursante>(),
										  provider.GetRequiredService<IServicioMateria>(),
										  provider.GetRequiredService<IServicioCurricula>(),
										  provider.GetRequiredService<IServicioDivision>(),
										  provider.GetRequiredService<IServicioAsistencia>(),
										  provider.GetRequiredService<IDialogService>(),
										  Navegacion<GestionDivisionesViewModel, MainWindowNavigationStore>(provider),
										  provider.GetRequiredService<CursoStore>(),
										  provider.GetRequiredService<DivisionStore>(),
										  provider.GetRequiredService<CicloLectivoStore>(), provider.GetRequiredService<ISesionActual>()));
		services.AddTransient<GestionCursosViewModel>(provider =>
			new GestionCursosViewModel(provider.GetRequiredService<IServicioCurso>(),
									   provider.GetRequiredService<IDialogService>(),
									   Navegacion<RegistrarCursosViewModel, MainWindowNavigationStore>(provider),
									   Navegacion<GestionDivisionesViewModel, MainWindowNavigationStore>(provider),
									   Navegacion<GestionCurriculasViewModel, MainWindowNavigationStore>(provider),
									   provider.GetRequiredService<CursoStore>(), provider.GetRequiredService<ISesionActual>()));
		// Cuatro destinos: cursos (volver), cursantes, asistencias y el modal de búsqueda de
		// docentes. Van tipados, así que invertir dos no compila.
		services.AddTransient<GestionDivisionesViewModel>(provider =>
			new GestionDivisionesViewModel(provider.GetRequiredService<IServicioDivision>(),
										   Navegacion<GestionCursosViewModel, MainWindowNavigationStore>(provider),
										   Navegacion<GestionCursantesViewModel, MainWindowNavigationStore>(provider),
										   Navegacion<GestionAsistenciasViewModel, MainWindowNavigationStore>(provider),
										   Navegacion<BuscarViewModel, ModalWindowNavigationStore>(provider),
										   provider.GetRequiredService<CursoStore>(),
										   provider.GetRequiredService<DivisionStore>(),
										   provider.GetRequiredService<CicloLectivoStore>(),
										   provider.GetRequiredService<BuscarContextoStore>(),
										   provider.GetRequiredService<IDialogService>(),
										   provider.GetRequiredService<IMessenger>(), provider.GetRequiredService<ISesionActual>()));

		// La raíz de la sección. Depende de GestionAsistenciasNavigationService y no al revés:
		// escribe CursoStore y DivisionStore y recién después navega a la planilla, que los lee
		// en su constructor.
		services.AddTransient<AsistenciasViewModel>(provider =>
			new AsistenciasViewModel(provider.GetRequiredService<IServicioCurso>(),
									 provider.GetRequiredService<IServicioDivision>(),
									 provider.GetRequiredService<IDialogService>(),
									 Navegacion<GestionAsistenciasViewModel, MainWindowNavigationStore>(provider),
									 provider.GetRequiredService<CursoStore>(),
									 provider.GetRequiredService<DivisionStore>(),
									 provider.GetRequiredService<CicloLectivoStore>()));

		services.AddTransient<GestionAsistenciasViewModel>(provider =>
			new GestionAsistenciasViewModel(Navegacion<GestionDivisionesViewModel, MainWindowNavigationStore>(provider),
											provider.GetRequiredService<IServicioAsistencia>(),
											provider.GetRequiredService<IServicioCursante>(),
											provider.GetRequiredService<IServicioDivision>(),
											provider.GetRequiredService<CursoStore>(),
											provider.GetRequiredService<DivisionStore>(),
											provider.GetRequiredService<CicloLectivoStore>(),
											provider.GetRequiredService<IDialogService>(), provider.GetRequiredService<ISesionActual>()));
		#endregion

		#region Materias
		services.AddTransient<GestionCurriculasViewModel>(provider =>
			new GestionCurriculasViewModel(Navegacion<GestionCursosViewModel, MainWindowNavigationStore>(provider),
										 Navegacion<GestionCatedrasViewModel, MainWindowNavigationStore>(provider),
										 provider.GetRequiredService<IServicioCurricula>(),
										 provider.GetRequiredService<IServicioMateria>(),
										 provider.GetRequiredService<IDialogService>(),
										 provider.GetRequiredService<CursoStore>(),
										 provider.GetRequiredService<MateriaStore>(), provider.GetRequiredService<ISesionActual>()));
		#endregion

		#region Catedras
		// Tres destinos: diseño curricular (volver), situación de revista y horarios. Van tipados,
		// así que invertir dos no compila.
		services.AddTransient<GestionCatedrasViewModel>(provider =>
			new GestionCatedrasViewModel(Navegacion<GestionCurriculasViewModel, MainWindowNavigationStore>(provider),
										 Navegacion<GestionSituacionRevistaViewModel, MainWindowNavigationStore>(provider),
										 Navegacion<GestionHorariosViewModel, MainWindowNavigationStore>(provider),
										 provider.GetRequiredService<IServicioCatedra>(),
										 provider.GetRequiredService<IServicioDivision>(),
										 provider.GetRequiredService<MateriaStore>(),
										 provider.GetRequiredService<CatedraStore>(),
										 provider.GetRequiredService<CicloLectivoStore>(),
										 provider.GetRequiredService<IDialogService>(), provider.GetRequiredService<ISesionActual>()));

		services.AddTransient<GestionHorariosViewModel>(provider =>
			new GestionHorariosViewModel(Navegacion<GestionCatedrasViewModel, MainWindowNavigationStore>(provider),
										 provider.GetRequiredService<IServicioCatedra>(),
										 provider.GetRequiredService<MateriaStore>(),
										 provider.GetRequiredService<CatedraStore>(),
										 provider.GetRequiredService<IDialogService>(), provider.GetRequiredService<ISesionActual>()));
		#endregion

		#region SituaciónRevista
		services.AddTransient<GestionSituacionRevistaViewModel>(provider =>
			new GestionSituacionRevistaViewModel(Navegacion<GestionCatedrasViewModel, MainWindowNavigationStore>(provider),
												 Navegacion<BuscarViewModel, ModalWindowNavigationStore>(provider),
												 provider.GetRequiredService<IServicioDocente>(),
												 provider.GetRequiredService<IServicioCatedra>(),
												 provider.GetRequiredService<IDialogService>(),
												 provider.GetRequiredService<CursoStore>(),
												 provider.GetRequiredService<MateriaStore>(),
												 provider.GetRequiredService<CatedraStore>(),
												 provider.GetRequiredService<BuscarContextoStore>(),
												 provider.GetRequiredService<IMessenger>(), provider.GetRequiredService<ISesionActual>()));
		#endregion

		#region Documentación y reportes
		services.AddTransient<GestionDocumentacionViewModel>(provider =>
			new GestionDocumentacionViewModel(provider.GetRequiredService<IServicioAlumno>(),
											  provider.GetRequiredService<IServicioDocumento>(),
											  provider.GetRequiredService<IArchivoService>(),
											  provider.GetRequiredService<IDialogService>(),
											  provider.GetRequiredService<CicloLectivoStore>(), provider.GetRequiredService<ISesionActual>()));

		services.AddTransient<GestionReportesViewModel>(provider =>
			new GestionReportesViewModel(provider.GetRequiredService<IServicioReporte>(),
										 provider.GetRequiredService<IServicioCurso>(),
										 provider.GetRequiredService<IServicioDivision>(),
										 provider.GetRequiredService<IArchivoService>(),
										 provider.GetRequiredService<IDialogService>(),
										 provider.GetRequiredService<CicloLectivoStore>(), provider.GetRequiredService<ISesionActual>()));
		#endregion

		#region Usuarios
		services.AddTransient<RegistrarUsuarioViewModel>();

		services.AddTransient<LoginUsuarioViewModel>(provider =>
			new LoginUsuarioViewModel(Navegacion<MainViewModel, MainWindowNavigationStore>(provider),
									  provider.GetRequiredService<IServicioAutenticacion>(),
									  provider.GetRequiredService<IDialogService>(),
									  provider.GetRequiredService<ISesionUsuario>()));
		#endregion

		return services;
	}
	#endregion

	#region NavigationServices
	private static IServiceCollection AddNavigationServices(IServiceCollection services)
	{
		services.AddSingleton<IDialogService, DialogService>();
		services.AddSingleton<IArchivoService, ArchivoService>();
		services.AddSingleton<INavigationService>(provider =>
			Navegacion<MainViewModel, MainWindowNavigationStore>(provider));

		return services;
	}
	#endregion


	#region Navegación
	/// <summary>
	/// Un servicio de navegación hacia <typeparamref name="TViewModel"/> en la ventana que representa
	/// <typeparamref name="TStore"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Reemplaza a las 26 factories <c>Create…NavigationService</c></strong> que había acá, una
	/// por destino, cada una construyendo una clase distinta con el mismo cuerpo. El destino ya no es el
	/// nombre de una clase sino los dos parámetros de tipo, que se leen en el lugar de la llamada: antes
	/// <c>Navegacion<GestionDocentesViewModel, MainWindowNavigationStore>(provider)</c> nombraba una clase y había que abrirla
	/// para saber a qué ventana escribía; ahora
	/// <c>Navegacion&lt;GestionDocentesViewModel, MainWindowNavigationStore&gt;(provider)</c> lo dice
	/// entero.
	/// </para>
	/// <para>
	/// <strong>El <c>provider</c> que entra acá puede ser el raíz o el de un scope, y las dos cosas están
	/// bien.</strong> Lo único que se le pide son dos registros <c>Singleton</c>
	/// —<see cref="IServiceScopeFactory"/> y el store—, que resuelven a la misma instancia desde
	/// cualquier proveedor. Esto es lo que permite que un ViewModel nacido en un scope reciba servicios
	/// de navegación que le sobreviven: si el servicio guardara algo <c>Scoped</c>, navegar por segunda
	/// vez desde una pantalla daría <c>ObjectDisposedException</c>.
	/// </para>
	/// </remarks>
	private static INavigationService<TViewModel> Navegacion<TViewModel, TStore>(IServiceProvider provider)
		where TViewModel : ObservableObject
		where TStore : StoreDeNavegacion =>
		new NavigationService<TViewModel, TStore>(provider.GetRequiredService<IServiceScopeFactory>(),
												  provider.GetRequiredService<TStore>());
	#endregion
}
