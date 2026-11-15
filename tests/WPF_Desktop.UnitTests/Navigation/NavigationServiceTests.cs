using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WPF_Desktop.Navigation;
using WPF_Desktop.Store.NavigationStore;
using Xunit;

namespace WPF_Desktop.UnitTests.Navigation;

/// <summary>
/// Fija lo que hace <c>NavigationService&lt;TViewModel, TStore&gt;</c>, que reemplazó a las 26 clases
/// de navegación: cada navegación estrena un <see cref="IServiceScope"/> y la pantalla sale de ahí.
/// </summary>
/// <remarks>
/// Es la prueba del scope por pantalla del lado de la navegación. El contenedor real no sirve para afirmarlo —un
/// <c>IServicio*</c> resuelto de dos scopes distintos da dos instancias, pero eso lo garantiza el
/// contenedor de Microsoft y no este código—; lo que hay que afirmar es que <strong>este servicio
/// efectivamente crea un scope por navegación y resuelve de ahí</strong>, porque el defecto original
/// era precisamente resolver del proveedor raíz.
/// </remarks>
public class NavigationServiceTests
{
	#region Dobles
	private sealed class PantallaFalsa : ObservableObject
	{
	}

	private sealed class ScopeFalso : IServiceScope
	{
		public ScopeFalso(IServiceProvider proveedor) => ServiceProvider = proveedor;

		public bool Liberado { get; private set; }

		public IServiceProvider ServiceProvider { get; }

		public void Dispose() => Liberado = true;
	}

	/// <summary>
	/// Cuenta los scopes que le pidieron y los deja a mano para inspeccionarlos.
	/// </summary>
	private sealed class FabricaDeScopesFalsa : IServiceScopeFactory
	{
		private readonly Func<IServiceProvider> _proveedor;

		public FabricaDeScopesFalsa(Func<IServiceProvider> proveedor) => _proveedor = proveedor;

		public List<ScopeFalso> Creados { get; } = new();

		public IServiceScope CreateScope()
		{
			var scope = new ScopeFalso(_proveedor());
			Creados.Add(scope);

			return scope;
		}
	}

	/// <summary>Resuelve una sola cosa: el ViewModel que se le configuró, o lanza.</summary>
	private sealed class ProveedorFalso : IServiceProvider
	{
		private readonly Func<object> _resolver;

		public ProveedorFalso(Func<object> resolver) => _resolver = resolver;

		public object? GetService(Type serviceType) => _resolver();
	}

	private static FabricaDeScopesFalsa FabricaQueResuelve(Func<ObservableObject> pantalla) =>
		new(() => new ProveedorFalso(pantalla));
	#endregion

	#region Un scope por navegación
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Navegar_crea_un_scope()
	{
		var fabrica = FabricaQueResuelve(() => new PantallaFalsa());
		var store = new MainWindowNavigationStore();
		var servicio = new NavigationService<PantallaFalsa, MainWindowNavigationStore>(fabrica, store);

		servicio.Navigate();

		fabrica.Creados.Count.ShouldBe(1);
	}

	/// <remarks>
	/// El corazón del scope por pantalla: si los ViewModels salieran del proveedor raíz, todas las pantallas
	/// compartirían un <c>DbContext</c> de vida de aplicación. Dos navegaciones tienen que dar dos scopes.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Cada_navegacion_estrena_su_propio_scope()
	{
		var fabrica = FabricaQueResuelve(() => new PantallaFalsa());
		var store = new MainWindowNavigationStore();
		var servicio = new NavigationService<PantallaFalsa, MainWindowNavigationStore>(fabrica, store);

		servicio.Navigate();
		servicio.Navigate();

		fabrica.Creados.Count.ShouldBe(2);
		fabrica.Creados[1].ShouldNotBeSameAs(fabrica.Creados[0]);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_pantalla_sale_del_scope_recien_creado_y_queda_en_el_store()
	{
		var pantalla = new PantallaFalsa();
		var fabrica = FabricaQueResuelve(() => pantalla);
		var store = new MainWindowNavigationStore();
		var servicio = new NavigationService<PantallaFalsa, MainWindowNavigationStore>(fabrica, store);

		servicio.Navigate();

		store.ViewModel.ShouldBeSameAs(pantalla);
	}

	/// <remarks>
	/// La contracara de <c>StoreDeNavegacionTests.Reemplazar_libera_el_scope_de_la_pantalla_anterior</c>,
	/// vista desde acá: navegar dos veces libera el scope de la primera y deja vivo el de la segunda.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Navegar_de_nuevo_libera_el_scope_de_la_pantalla_que_se_va()
	{
		var fabrica = FabricaQueResuelve(() => new PantallaFalsa());
		var store = new MainWindowNavigationStore();
		var servicio = new NavigationService<PantallaFalsa, MainWindowNavigationStore>(fabrica, store);

		servicio.Navigate();
		servicio.Navigate();

		fabrica.Creados[0].Liberado.ShouldBeTrue();
		fabrica.Creados[1].Liberado.ShouldBeFalse();
	}
	#endregion

	#region La pantalla no se pudo construir
	/// <remarks>
	/// Sin el <c>try</c> de <c>Navigate</c> el scope quedaría huérfano —nadie más tiene una
	/// referencia— y con él su <c>DbContext</c>: una conexión filtrada por cada intento fallido de
	/// navegar. Los constructores de los ViewModels leen stores y resuelven servicios, así que pueden
	/// lanzar de verdad.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Si_la_pantalla_no_se_puede_construir_el_scope_no_queda_huerfano()
	{
		var fabrica = new FabricaDeScopesFalsa(
			() => new ProveedorFalso(() => throw new InvalidOperationException("no se pudo construir")));
		var store = new MainWindowNavigationStore();
		var servicio = new NavigationService<PantallaFalsa, MainWindowNavigationStore>(fabrica, store);

		Should.Throw<InvalidOperationException>(() => servicio.Navigate());

		fabrica.Creados.Count.ShouldBe(1);
		fabrica.Creados[0].Liberado.ShouldBeTrue();
	}

	/// <remarks>
	/// La excepción se propaga y no se traga: tragársela dejaría la pantalla sin cambiar y sin
	/// explicación.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Si_la_pantalla_no_se_puede_construir_el_store_no_cambia_y_la_excepcion_sube()
	{
		var pantallaPrevia = new PantallaFalsa();
		var store = new MainWindowNavigationStore();
		store.Reemplazar(pantallaPrevia, null);
		var fabrica = new FabricaDeScopesFalsa(
			() => new ProveedorFalso(() => throw new InvalidOperationException("no se pudo construir")));
		var servicio = new NavigationService<PantallaFalsa, MainWindowNavigationStore>(fabrica, store);

		Should.Throw<InvalidOperationException>(() => servicio.Navigate());

		store.ViewModel.ShouldBeSameAs(pantallaPrevia);
	}
	#endregion

	#region Los tres destinos
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_destino_modal_escribe_en_el_store_del_modal()
	{
		var pantalla = new PantallaFalsa();
		var fabrica = FabricaQueResuelve(() => pantalla);
		var storeModal = new ModalWindowNavigationStore();
		var servicio = new NavigationService<PantallaFalsa, ModalWindowNavigationStore>(fabrica, storeModal);

		servicio.Navigate();

		storeModal.EstaAbierto.ShouldBeTrue();
		storeModal.ViewModel.ShouldBeSameAs(pantalla);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_destino_de_acceso_escribe_en_el_store_de_la_ventana_de_acceso()
	{
		var pantalla = new PantallaFalsa();
		var fabrica = FabricaQueResuelve(() => pantalla);
		var storeAcceso = new StartupWindowNavigationStore();
		var servicio = new NavigationService<PantallaFalsa, StartupWindowNavigationStore>(fabrica, storeAcceso);

		servicio.Navigate();

		storeAcceso.ViewModel.ShouldBeSameAs(pantalla);
	}
	#endregion
}
