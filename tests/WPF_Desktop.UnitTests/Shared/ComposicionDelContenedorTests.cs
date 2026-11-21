using Core.ServicioDocentes;
using Core.Shared;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.Store.NavigationStore;
using WPF_Desktop.ViewModels.Asistencias;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.Catedras;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Usuarios;
using Xunit;

namespace WPF_Desktop.UnitTests.Shared;

/// <summary>
/// Reproduce la cadena de registro de <c>App.xaml.cs</c> y verifica que los Singleton de la capa
/// de presentación sean una sola instancia por raíz. Es la prueba que habría detectado E.2
/// (dos contenedores sobre la misma IServiceCollection) sin levantar la UI.
/// </summary>
public class ComposicionDelContenedorTests
{
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

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_contenedor_del_host_se_construye_sin_base_de_datos()
	{
		IHost? host = null;
		Exception? error = null;

		try
		{
			host = ConstruirHost();
		}
		catch (Exception ex)
		{
			error = ex;
		}

		error.ShouldBeNull();
		host.ShouldNotBeNull();
		host!.Dispose();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void MainWindowNavigationStore_es_la_misma_instancia_al_resolverse_dos_veces()
	{
		using var host = ConstruirHost();

		var primera = host.Services.GetRequiredService<MainWindowNavigationStore>();
		var segunda = host.Services.GetRequiredService<MainWindowNavigationStore>();

		segunda.ShouldBeSameAs(primera);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void StartupWindowNavigationStore_es_la_misma_instancia_al_resolverse_dos_veces()
	{
		using var host = ConstruirHost();

		var primera = host.Services.GetRequiredService<StartupWindowNavigationStore>();
		var segunda = host.Services.GetRequiredService<StartupWindowNavigationStore>();

		segunda.ShouldBeSameAs(primera);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void ModalWindowNavigationStore_es_la_misma_instancia_al_resolverse_dos_veces()
	{
		using var host = ConstruirHost();

		var primera = host.Services.GetRequiredService<ModalWindowNavigationStore>();
		var segunda = host.Services.GetRequiredService<ModalWindowNavigationStore>();

		segunda.ShouldBeSameAs(primera);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void CicloLectivoStore_es_la_misma_instancia_al_resolverse_dos_veces()
	{
		using var host = ConstruirHost();

		var primera = host.Services.GetRequiredService<CicloLectivoStore>();
		var segunda = host.Services.GetRequiredService<CicloLectivoStore>();

		segunda.ShouldBeSameAs(primera);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void CatedraStore_es_la_misma_instancia_al_resolverse_dos_veces()
	{
		using var host = ConstruirHost();

		var primera = host.Services.GetRequiredService<CatedraStore>();
		var segunda = host.Services.GetRequiredService<CatedraStore>();

		segunda.ShouldBeSameAs(primera);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_cambio_escrito_en_un_store_lo_ve_quien_lo_resuelve_despues()
	{
		using var host = ConstruirHost();
		var escritor = host.Services.GetRequiredService<CicloLectivoStore>();
		escritor.CicloLectivo = "2031";

		var lector = host.Services.GetRequiredService<CicloLectivoStore>();

		lector.CicloLectivo.ShouldBe("2031");
	}

	#region Pantallas nuevas
	/// <summary>
	/// Resuelve del host los dos ViewModels de las pantallas nuevas (B6 y B7 del plan de casos de
	/// uso pendientes).
	/// </summary>
	/// <remarks>
	/// <para>
	/// Los tests de arriba NO cubren esto, y es importante entender por qué: construir el host sólo
	/// valida que la <c>IServiceCollection</c> se arme, y las factory lambda de los
	/// <c>AddTransient</c> se evalúan recién cuando alguien <b>resuelve</b> el tipo. Un
	/// <c>AddTransient&lt;XViewModel&gt;</c> que falta, o una lambda que pide un servicio no
	/// registrado, pasa desapercibido hasta que el usuario abre esa pantalla exacta y la ve en
	/// blanco. La única forma de detectarlo sin levantar la UI es resolver el ViewModel acá.
	/// </para>
	/// <para>
	/// Los stores se siembran antes de resolver porque los ViewModels leen el parámetro de
	/// navegación <b>en el constructor</b> —es la convención de este repo, ver el comentario de
	/// <c>CursoStore</c>—, así que un store vacío revienta en el <c>new</c> por una razón que no
	/// tiene nada que ver con lo que esta prueba quiere afirmar.
	/// </para>
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void LicenciasPendientesViewModel_se_resuelve_del_contenedor_real()
	{
		using var host = ConstruirHost();

		var viewModel = host.Services.GetRequiredService<WPF_Desktop.ViewModels.Licencias.LicenciasPendientesViewModel>();

		viewModel.ShouldNotBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void GestionAsistenciasViewModel_se_resuelve_del_contenedor_real()
	{
		using var host = ConstruirHost();
		host.Services.GetRequiredService<CursoStore>().Curso = new CursoSeleccionado(Guid.NewGuid(), "Primero", "Secundario");
		host.Services.GetRequiredService<DivisionStore>().Division = new DivisionSeleccionada(Guid.NewGuid());

		var viewModel = host.Services.GetRequiredService<GestionAsistenciasViewModel>();

		viewModel.ShouldNotBeNull();
	}

	/// <remarks>
	/// A diferencia de <c>GestionAsistenciasViewModel</c>, esta <b>no</b> siembra los stores: es
	/// la raíz de la sección y su trabajo es justamente escribirlos. Si algún día necesitara que
	/// ya estuvieran puestos, dejaría de ser una raíz de menú y el rol <c>Preceptoria</c> volvería
	/// a quedarse sin forma de entrar.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void AsistenciasViewModel_se_resuelve_del_contenedor_real_sin_sembrar_los_stores()
	{
		using var host = ConstruirHost();

		var viewModel = host.Services.GetRequiredService<AsistenciasViewModel>();

		viewModel.ShouldNotBeNull();
	}

	/// <remarks>
	/// <para>
	/// La séptima raíz del menú. Tampoco siembra stores: no usa ninguno, porque no
	/// es destino de ningún drill-down.
	/// </para>
	/// <para>
	/// Esta prueba es la que convierte "falta un registro de DI" en una prueba roja en lugar de una
	/// pantalla en blanco. Una vista nueva necesita cuatro piezas —<c>DataTemplate</c>,
	/// ViewModel, <c>INavigationService</c> y sus registros—; si falta el del ViewModel, acá revienta.
	/// </para>
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void GestionUsuariosViewModel_se_resuelve_del_contenedor_real()
	{
		using var host = ConstruirHost();

		var viewModel = host.Services.GetRequiredService<GestionUsuariosViewModel>();

		viewModel.ShouldNotBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void GestionHorariosViewModel_se_resuelve_del_contenedor_real()
	{
		using var host = ConstruirHost();
		host.Services.GetRequiredService<MateriaStore>().Materia = new MateriaSeleccionada(Guid.NewGuid(), Guid.NewGuid(), "Matematica");
		host.Services.GetRequiredService<CatedraStore>().Catedra = Guid.NewGuid();

		var viewModel = host.Services.GetRequiredService<GestionHorariosViewModel>();

		viewModel.ShouldNotBeNull();
	}
	#endregion

	#region Alcance de los servicios de Core
	/// <summary>
	/// Dos pantallas distintas no comparten los servicios de <c>Core</c> ni, por lo tanto, el
	/// <c>DbContext</c> que hay detrás.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Afirma el scope por pantalla sobre el grafo de DI <strong>real</strong>. Los servicios de <c>Core</c> están
	/// registrados <c>Scoped</c> en <c>CoreDI</c>, pero si todos los ViewModels se
	/// resolvieran del proveedor <strong>raíz</strong>, donde <c>Scoped</c> equivale a una instancia por
	/// proceso, habría un único <c>DbContext</c> para toda la aplicación, con
	/// <c>"a second operation was started on this context"</c> si dos cargas se solapaban y un
	/// <c>ChangeTracker</c> que crecía sin tope.
	/// </para>
	/// <para>
	/// Lo que esta prueba <strong>no</strong> acredita es que el código de producción cree esos scopes
	/// —eso lo fija <c>NavigationServiceTests</c>—; acredita que crearlos sirva de algo, o sea que el
	/// registro siga siendo <c>Scoped</c>. Pasar <c>CoreDI</c> a <c>Singleton</c> rompería el aislamiento por pantalla sin
	/// tocar una línea de la navegación, y esta prueba es la única que se enteraría.
	/// </para>
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_servicio_de_Core_resuelto_en_dos_scopes_da_dos_instancias()
	{
		using var host = ConstruirHost();
		var fabrica = host.Services.GetRequiredService<IServiceScopeFactory>();

		using var primerScope = fabrica.CreateScope();
		using var segundoScope = fabrica.CreateScope();
		var primero = primerScope.ServiceProvider.GetRequiredService<IServicioDocente>();
		var segundo = segundoScope.ServiceProvider.GetRequiredService<IServicioDocente>();

		segundo.ShouldNotBeSameAs(primero);
	}

	/// <remarks>
	/// La contracara: los <c>*Store</c> son <c>Singleton</c> y tienen que seguir siendo la misma
	/// instancia vistos desde scopes distintos. Si un scope los duplicara, el scope por pantalla
	/// habría roto el paso de parámetros entre pantallas —que es justamente lo que los stores hacen— y
	/// el barrido de <c>App.LimpiarEstadoDeSesion</c>, que los resuelve de la raíz, limpiaría copias
	/// que nadie lee.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_store_visto_desde_dos_scopes_sigue_siendo_la_misma_instancia()
	{
		using var host = ConstruirHost();
		var fabrica = host.Services.GetRequiredService<IServiceScopeFactory>();

		using var primerScope = fabrica.CreateScope();
		using var segundoScope = fabrica.CreateScope();
		var desdeElPrimero = primerScope.ServiceProvider.GetRequiredService<MainWindowNavigationStore>();
		var desdeElSegundo = segundoScope.ServiceProvider.GetRequiredService<MainWindowNavigationStore>();
		var desdeLaRaiz = host.Services.GetRequiredService<MainWindowNavigationStore>();

		desdeElSegundo.ShouldBeSameAs(desdeElPrimero);
		desdeLaRaiz.ShouldBeSameAs(desdeElPrimero);
	}

	/// <remarks>
	/// Un ViewModel tiene que poder resolverse de un scope, que es de donde lo resuelve
	/// <c>NavigationService</c>. Parece obvio y no lo es: un ViewModel cuya factory en
	/// <c>WPF_DesktopDI</c> resolviera algo registrado <c>Singleton</c> contra un
	/// <c>IServiceProvider</c> de scope seguiría funcionando, pero uno que dependiera de algo no
	/// registrado fallaría sólo por este camino.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_view_model_se_resuelve_desde_un_scope_y_no_solo_desde_la_raiz()
	{
		using var host = ConstruirHost();
		var fabrica = host.Services.GetRequiredService<IServiceScopeFactory>();

		using var scope = fabrica.CreateScope();
		var viewModel = scope.ServiceProvider.GetRequiredService<GestionUsuariosViewModel>();

		viewModel.ShouldNotBeNull();
	}
	#endregion
}
