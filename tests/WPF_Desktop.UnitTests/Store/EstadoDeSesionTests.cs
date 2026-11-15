using Core.Shared;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using System.Reflection;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using Xunit;

namespace WPF_Desktop.UnitTests.Store;

/// <summary>
/// El contrato de limpieza que hace posible el cambio de usuario: los <c>*Store</c> son
/// <c>Singleton</c> —uno por proceso— y desde que existe el relogin la sesión dura menos que el
/// proceso, así que lo que un usuario deja en un store lo encuentra el que entra después.
/// </summary>
/// <remarks>
/// Las dos primeras pruebas son guardas de convención y son las que importan: un store nuevo que no
/// implemente <see cref="IEstadoDeSesion"/>, o que lo implemente y no se registre, filtra estado
/// entre usuarios <b>sin un solo error</b> —ni de compilación ni en ejecución—. No hay manera de
/// que una prueba de comportamiento sobre un store concreto se entere de eso: lo que falla es un
/// store que nadie escribió todavía.
/// </remarks>
public class EstadoDeSesionTests
{
	/// <summary>
	/// Los <c>*Store</c> que guardan estado del usuario. Excluye los <c>*NavigationStore</c>: lo que
	/// guardan es el ViewModel activo de una ventana, y su reinicio lo hace <c>App</c> aparte,
	/// atado al ciclo de vida de esa ventana.
	/// </summary>
	private static IReadOnlyList<Type> StoresDeEstado =>
		typeof(CursoStore).Assembly
						  .GetTypes()
						  .Where(tipo => tipo.IsClass && !tipo.IsAbstract)
						  .Where(tipo => tipo.Name.EndsWith("Store")
									  && !tipo.Name.EndsWith("NavigationStore"))
						  .OrderBy(tipo => tipo.Name)
						  .ToList();

	private static IHost ConstruirHost()
	{
		// Igual que App.xaml.cs: Host.CreateDefaultBuilder + Infrastructure -> Core -> WPF_Desktop.
		return Host.CreateDefaultBuilder()
			.ConfigureServices((contexto, services) =>
			{
				InfrastructureDI.AddInfrastructure(services);
				CoreDI.AddServices(services);
				WPF_DesktopDI.AddWPFDesktopDI(services);
			})
			.Build();
	}

	#region Guardas de convención
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todo_store_que_guarda_estado_del_usuario_implementa_IEstadoDeSesion()
	{
		StoresDeEstado.ShouldNotBeEmpty();

		var sinImplementar = StoresDeEstado.Where(tipo => !typeof(IEstadoDeSesion).IsAssignableFrom(tipo))
										   .Select(tipo => tipo.Name)
										   .ToList();

		sinImplementar.ShouldBeEmpty(
			"Estos stores no se barren al cambiar de usuario, así que lo que guardan sobrevive a la "
		  + "sesión: " + string.Join(", ", sinImplementar));
	}

	/// <summary>
	/// La prueba que atrapa el error de registro, que es el peligroso porque compila y no falla.
	/// </summary>
	/// <remarks>
	/// Escribir <c>AddSingleton&lt;IEstadoDeSesion, CursoStore&gt;()</c> en vez de resolver HACIA la
	/// instancia ya registrada construye un <b>segundo</b> <c>CursoStore</c>: las pantallas
	/// escribirían en el que les inyecta el contenedor y el barrido limpiaría el otro. Es el mismo
	/// defecto de forma que el doble registro de la sesión —<c>ISesionUsuario</c> /
	/// <c>ISesionActual</c>— que <c>WPF_DesktopDI</c> documenta en sus tres líneas, y la única
	/// manera de verlo es comparar por REFERENCIA contra el store concreto.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Cada_store_se_resuelve_como_IEstadoDeSesion_y_es_la_misma_instancia_que_la_concreta()
	{
		using var host = ConstruirHost();

		var barridos = host.Services.GetServices<IEstadoDeSesion>().ToList();

		// Uno por store: ni de menos (un store sin registrar no se limpia) ni de más (un registro
		// repetido lo limpiaría dos veces, que es inocuo, pero delata un copiar y pegar).
		barridos.Count.ShouldBe(StoresDeEstado.Count);

		foreach (var tipo in StoresDeEstado)
		{
			var concreto = host.Services.GetRequiredService(tipo);

			barridos.ShouldContain(barrido => ReferenceEquals(barrido, concreto),
				customMessage: $"{tipo.Name} no está en el barrido con la MISMA instancia que inyecta "
							 + "el contenedor: hay dos instancias y se limpia la que nadie usa.");
		}
	}
	#endregion

	#region Limpiar devuelve a cada store a su valor de construcción
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Limpiar_el_legajo_borra_la_persona_elegida()
	{
		// Es el barrido que más importa: acá queda un alumno o un docente concreto, que es un dato
		// personal de un tercero y no una preferencia de pantalla.
		var store = new LegajoStore
		{
			PersonaID = Guid.NewGuid(),
			Documento = "30123456"
		};

		store.Limpiar();

		store.PersonaID.ShouldBeNull();
		store.Documento.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Limpiar_borra_el_curso_la_division_y_la_materia_del_drill_down()
	{
		var cursos = new CursoStore { Curso = new CursoSeleccionado(Guid.NewGuid(), "Primero", "Secundaria") };
		var divisiones = new DivisionStore { Division = new DivisionSeleccionada(Guid.NewGuid()) };
		var materias = new MateriaStore { Materia = new MateriaSeleccionada(Guid.NewGuid(), Guid.NewGuid(), "Matemática") };

		cursos.Limpiar();
		divisiones.Limpiar();
		materias.Limpiar();

		cursos.Curso.ShouldBeNull();
		divisiones.Division.ShouldBeNull();
		materias.Materia.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Limpiar_devuelve_la_catedra_al_centinela_vacio()
	{
		var store = new CatedraStore { Catedra = Guid.NewGuid() };

		store.Limpiar();

		store.Catedra.ShouldBe(Guid.Empty);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Limpiar_devuelve_el_ciclo_lectivo_al_ano_en_curso_y_no_al_que_dejo_el_usuario_anterior()
	{
		var store = new CicloLectivoStore { CicloLectivo = "2019" };

		store.Limpiar();

		store.CicloLectivo.ShouldBe(DateTime.Today.Year.ToString());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Limpiar_repone_el_contexto_generico_del_buscador()
	{
		// Un contexto DECLARADO y nunca tomado: Tomar() ya repone el genérico, así que este es el
		// único estado que podía quedar, y nombra una división concreta.
		var store = new BuscarContextoStore();
		store.Declarar(new BuscarContexto("Preceptor de primer año A", "Por apellido y nombre.", "Asignar preceptor"));

		store.Limpiar();

		store.Tomar().ShouldBe(BuscarContexto.Generico);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Limpiar_vacia_la_cola_de_avisos_al_pie()
	{
		// Un aviso cuenta el resultado de una operación de quien la hizo —"Alumno dado de baja"— y
		// no tiene por qué leerlo el usuario que entra después.
		var store = new AvisoStore();
		store.Informar("Alumno dado de baja");
		store.Advertir("No se pudo generar el certificado");

		store.Limpiar();

		store.Avisos.ShouldBeEmpty();
	}
	#endregion

	#region Las dos ventanas se pueden abrir más de una vez
	/// <summary>
	/// El cambio de usuario resuelve <c>StartupWindow</c> y <c>MainWindow</c> una vez por sesión, y
	/// en WPF una <c>Window</c> cerrada es terminal: <c>Show()</c> sobre ella tira
	/// <c>InvalidOperationException</c>. Con el registro anterior —<c>AddScoped</c> resuelto del
	/// proveedor raíz— había una sola instancia por proceso, que es lo que impedía volver a la
	/// pantalla de acceso.
	/// </summary>
	/// <remarks>
	/// La prueba no resuelve las ventanas: instanciar una <c>Window</c> exige un <c>Application</c>
	/// de WPF, que en el testhost no existe. Lo que verifica es el ciclo de vida DECLARADO, que es
	/// exactamente el dato que cambió y el único que se puede leer sin levantar la UI. Las dos se
	/// registran en <c>App.xaml.cs</c> y no en <c>WPF_DesktopDI</c>, así que se reproduce ese
	/// registro acá igual que <c>ComposicionDelContenedorTests</c> reproduce la cadena del host.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Las_ventanas_estan_registradas_Transient_para_poder_abrirse_una_vez_por_sesion()
	{
		var registros = typeof(global::WPF_Desktop.App)
			.GetMethod("ConfigureServices", BindingFlags.Instance | BindingFlags.NonPublic);

		registros.ShouldNotBeNull("ConfigureServices cambió de nombre o de visibilidad en App.");

		var fuente = Dobles.RaizDelRepositorio.Leer("WPF_Desktop/App.xaml.cs");

		fuente.ShouldContain("AddTransient(provider => new StartupWindow");
		fuente.ShouldContain("AddTransient(provider => new MainWindow");
		fuente.ShouldNotContain("AddScoped(provider => new StartupWindow");
		fuente.ShouldNotContain("AddScoped(provider => new MainWindow");
	}
	#endregion
}
