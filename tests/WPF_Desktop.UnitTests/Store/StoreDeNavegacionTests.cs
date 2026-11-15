using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WPF_Desktop.Store.NavigationStore;
using Xunit;

namespace WPF_Desktop.UnitTests.Store;

/// <summary>
/// Fija el ciclo de vida que los tres <c>*NavigationStore</c> heredan de <c>StoreDeNavegacion</c>:
/// al reemplazar la pantalla activa se libera la anterior y <strong>el scope de DI del que salió</strong>.
/// </summary>
/// <remarks>
/// <para>
/// Es la prueba del scope por pantalla y de su liberación del lado del store. Si nada libera lo
/// anterior, cada navegación deja atrás el ViewModel saliente y, con el scope por pantalla, también
/// su <c>DbContext</c>.
/// </para>
/// <para>
/// El caso que más importa acá es <see cref="Reemplazar_notifica_ANTES_de_liberar_lo_anterior"/>:
/// invertir esas dos operaciones no rompe ninguna otra prueba de esta clase y deja a la vista enlazada
/// por un instante a un ViewModel ya dispuesto, que es un defecto que sólo aparece corriendo la
/// aplicación.
/// </para>
/// </remarks>
public class StoreDeNavegacionTests
{
	#region Dobles
	/// <summary>Un ViewModel cualquiera, que además registra si lo liberaron.</summary>
	private sealed class PantallaFalsa : ObservableObject, IDisposable
	{
		public bool Liberada { get; private set; }

		public void Dispose() => Liberada = true;
	}

	/// <summary>Un ViewModel que NO es <see cref="IDisposable"/>, que es el caso de los 27 reales.</summary>
	private sealed class PantallaSinDisposeFalsa : ObservableObject
	{
	}

	/// <summary>
	/// Un <see cref="IServiceScope"/> de mentira: lo único que hace falta saber de él es si lo
	/// liberaron y en qué momento.
	/// </summary>
	private sealed class ScopeFalso : IServiceScope
	{
		private readonly Action? _alLiberar;

		public ScopeFalso(Action? alLiberar = null) => _alLiberar = alLiberar;

		public bool Liberado { get; private set; }

		public IServiceProvider ServiceProvider => throw new NotSupportedException(
			"Este doble no resuelve servicios: las pruebas del store sólo miran su liberación.");

		public void Dispose()
		{
			Liberado = true;
			_alLiberar?.Invoke();
		}
	}
	#endregion

	#region Reemplazo y liberación
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Reemplazar_deja_el_view_model_entrante_como_pantalla_activa()
	{
		var store = new MainWindowNavigationStore();
		var entrante = new PantallaFalsa();

		store.Reemplazar(entrante, new ScopeFalso());

		store.ViewModel.ShouldBeSameAs(entrante);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Reemplazar_libera_el_scope_de_la_pantalla_anterior()
	{
		var store = new MainWindowNavigationStore();
		var scopeViejo = new ScopeFalso();
		store.Reemplazar(new PantallaFalsa(), scopeViejo);

		store.Reemplazar(new PantallaFalsa(), new ScopeFalso());

		scopeViejo.Liberado.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Reemplazar_no_libera_el_scope_entrante()
	{
		var store = new MainWindowNavigationStore();
		store.Reemplazar(new PantallaFalsa(), new ScopeFalso());
		var scopeNuevo = new ScopeFalso();

		store.Reemplazar(new PantallaFalsa(), scopeNuevo);

		scopeNuevo.Liberado.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Reemplazar_libera_el_view_model_anterior_cuando_es_IDisposable()
	{
		var store = new MainWindowNavigationStore();
		var pantallaVieja = new PantallaFalsa();
		store.Reemplazar(pantallaVieja, new ScopeFalso());

		store.Reemplazar(new PantallaFalsa(), new ScopeFalso());

		pantallaVieja.Liberada.ShouldBeTrue();
	}

	/// <remarks>
	/// Los 27 ViewModels reales no implementan <see cref="IDisposable"/>, así que el camino normal es
	/// este: el store libera el scope y no toca el ViewModel. La prueba existe para que el día que
	/// alguno sí lo implemente no sea una sorpresa.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Reemplazar_libera_el_scope_aunque_el_view_model_no_sea_IDisposable()
	{
		var store = new MainWindowNavigationStore();
		var scopeViejo = new ScopeFalso();
		store.Reemplazar(new PantallaSinDisposeFalsa(), scopeViejo);

		store.Reemplazar(new PantallaSinDisposeFalsa(), new ScopeFalso());

		scopeViejo.Liberado.ShouldBeTrue();
	}

	/// <remarks>
	/// Es el defecto que invertir el orden de <c>Reemplazar</c> produce, y no lo ve ninguna otra prueba
	/// de esta clase: liberar antes de notificar deja a <c>MainViewModel.ViewModelActual</c>
	/// devolviendo un ViewModel ya dispuesto durante el <c>OnPropertyChanged</c>, así que cualquier
	/// binding que WPF evalúe en ese instante trabaja contra un <c>DbContext</c> cerrado.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Reemplazar_notifica_ANTES_de_liberar_lo_anterior()
	{
		var store = new MainWindowNavigationStore();
		var orden = new List<string>();
		store.Reemplazar(new PantallaFalsa(), new ScopeFalso(() => orden.Add("liberacion")));
		store.ViewModelChanged += () => orden.Add("notificacion");

		store.Reemplazar(new PantallaFalsa(), new ScopeFalso());

		orden.ShouldBe(new[] { "notificacion", "liberacion" });
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Reemplazar_por_lo_mismo_que_ya_estaba_no_libera_nada()
	{
		var store = new MainWindowNavigationStore();
		var pantalla = new PantallaFalsa();
		var scope = new ScopeFalso();
		store.Reemplazar(pantalla, scope);

		store.Reemplazar(pantalla, scope);

		scope.Liberado.ShouldBeFalse();
		pantalla.Liberada.ShouldBeFalse();
	}
	#endregion

	#region El setter de ViewModel y el barrido de fin de sesión
	/// <remarks>
	/// <c>App.LimpiarEstadoDeSesion</c> vacía los dos stores de ventana con
	/// <c>ViewModel = null!</c>, y ese camino tiene que liberar el scope igual que una navegación:
	/// es lo que hace que un cambio de usuario no se lleve puesto el <c>DbContext</c> de la pantalla
	/// que estaba abierta.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Vaciar_el_store_por_el_setter_libera_el_scope_de_la_pantalla_activa()
	{
		var store = new MainWindowNavigationStore();
		var scope = new ScopeFalso();
		store.Reemplazar(new PantallaFalsa(), scope);

		store.ViewModel = null!;

		scope.Liberado.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_el_view_model_por_el_setter_lo_deja_como_pantalla_activa()
	{
		var store = new StartupWindowNavigationStore();
		var pantalla = new PantallaFalsa();

		store.ViewModel = pantalla;

		store.ViewModel.ShouldBeSameAs(pantalla);
	}
	#endregion

	#region Modal
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_modal_sin_view_model_esta_cerrado()
	{
		var store = new ModalWindowNavigationStore();

		store.EstaAbierto.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_modal_con_view_model_esta_abierto()
	{
		var store = new ModalWindowNavigationStore();

		store.Reemplazar(new PantallaFalsa(), new ScopeFalso());

		store.EstaAbierto.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Cerrar_el_modal_libera_su_scope_y_lo_deja_cerrado()
	{
		var store = new ModalWindowNavigationStore();
		var scope = new ScopeFalso();
		store.Reemplazar(new PantallaFalsa(), scope);

		store.Cerrar();

		scope.Liberado.ShouldBeTrue();
		store.EstaAbierto.ShouldBeFalse();
	}
	#endregion
}
