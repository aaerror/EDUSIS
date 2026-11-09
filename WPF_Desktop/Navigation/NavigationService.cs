using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using WPF_Desktop.Store.NavigationStore;

namespace WPF_Desktop.Navigation;

/// <summary>
/// Navega a <typeparamref name="TViewModel"/> dentro de la ventana que representa
/// <typeparamref name="TStore"/>, dándole a esa pantalla <strong>su propio
/// <see cref="IServiceScope"/></strong> y por lo tanto su propio <c>DbContext</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Reemplaza a las 26 clases de navegación que tenían este mismo cuerpo</strong>
/// (<c>GestionDocentesNavigationService</c>, <c>RegistrarCursoNavigationService</c>, …), que se
/// distinguían sólo por el nombre y por cuál de los tres stores recibían. Lo que antes era el nombre
/// de la clase ahora son los dos parámetros de tipo. Es la primera mitad de UI-50; <strong>la segunda
/// sigue abierta</strong>: como todos se inyectan en los ViewModels con el tipo
/// <see cref="INavigationService"/>, invertir dos parámetros de un constructor sigue compilando y
/// sigue navegando a otro lado. Cerrarla exige tipar la interfaz por destino.
/// </para>
/// <para>
/// <strong>UI-18: por qué un scope por pantalla.</strong> Los servicios de <c>Core</c> están
/// registrados <c>Scoped</c> en <c>CoreDI</c>, igual que el <c>DbContext</c> y el <c>IUnitOfWork</c>
/// de <c>InfrastructureDI</c>. Antes de esto, los ViewModels se resolvían del proveedor
/// <strong>raíz</strong>, donde "scoped" equivale a "una instancia por proceso": todas las pantallas
/// compartían un único <c>DbContext</c> de vida de aplicación. Los dos síntomas registrados eran
/// <c>"a second operation was started on this context"</c> si dos cargas se solapaban, y un
/// <c>ChangeTracker</c> que crecía sin tope dejando datos obsoletos en pantalla.
/// </para>
/// <para>
/// <strong>Este servicio sólo puede guardar dependencias Singleton, y eso es una restricción real del
/// diseño.</strong> <see cref="IServiceScopeFactory"/> y los tres stores lo son. Si guardara algo
/// <c>Scoped</c> quedaría atado al scope de la pantalla que lo construyó y moriría con ella —y una
/// pantalla navega justamente <em>después</em> de haber nacido, así que el defecto aparecería como un
/// <c>ObjectDisposedException</c> al segundo salto—. Por eso el scope se crea acá en cada
/// <see cref="Navigate"/> y no se guarda: cada navegación estrena uno.
/// </para>
/// </remarks>
internal sealed class NavigationService<TViewModel, TStore> : INavigationService
	where TViewModel : ObservableObject
	where TStore : StoreDeNavegacion
{
	private readonly IServiceScopeFactory _fabricaDeScopes;
	private readonly TStore _store;


	public NavigationService(IServiceScopeFactory fabricaDeScopes, TStore store)
	{
		_fabricaDeScopes = fabricaDeScopes;
		_store = store;
	}

	/// <remarks>
	/// El <c>try</c> cubre el caso en que el ViewModel no se pueda construir: sus constructores leen
	/// stores y resuelven servicios, así que pueden lanzar. Sin esto el scope quedaría huérfano —nadie
	/// más tiene una referencia— y con él su <c>DbContext</c>, una conexión filtrada por cada intento
	/// fallido de navegar. La excepción se propaga igual: tragársela dejaría la pantalla sin cambiar y
	/// sin explicación, que es la familia de defectos de H-018.
	/// </remarks>
	public void Navigate()
	{
		var scope = _fabricaDeScopes.CreateScope();

		try
		{
			_store.Reemplazar(scope.ServiceProvider.GetRequiredService<TViewModel>(), scope);
		}
		catch
		{
			scope.Dispose();
			throw;
		}
	}
}
