using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace WPF_Desktop.Store.NavigationStore;

/// <summary>
/// Base de los tres stores de navegación: guarda el ViewModel activo de una ventana y
/// también <strong>el <see cref="IServiceScope"/> del que ese ViewModel salió</strong>, para poder
/// liberarlo cuando se navega a otra pantalla.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Por qué el scope vive acá y no en el servicio de navegación.</strong> Quien crea el scope
/// es <c>NavigationService</c>, pero quien sabe <em>cuándo deja de hacer falta</em> es el store: el
/// scope anterior tiene que sobrevivir hasta que llegue el reemplazo, porque hasta ese momento la
/// pantalla sigue dibujada y enlazada a su ViewModel. El servicio de navegación no tiene forma de
/// saberlo —navega y se olvida—, así que el dueño del ciclo de vida es el store.
/// </para>
/// <para>
/// <strong>El nombre no es <c>NavigationStore</c> a propósito.</strong> Esta clase vive en el
/// namespace <c>WPF_Desktop.Store.NavigationStore</c>, así que un tipo con ese nombre haría que el
/// nombre simple se resolviera al namespace y no al tipo (<c>CS0118</c>). Es la misma trampa que
/// <c>Core/Shared/CoreDI.cs</c> documenta para <c>ServicioSeguridad</c>.
/// </para>
/// <para>
/// <strong>Disposal del saliente.</strong> Reemplazar el ViewModel sin disposal ni desuscripción
/// dejaría fugas: lo único que las impediría es que <c>WeakReferenceMessenger</c> usa referencias
/// débiles. Por eso <see cref="Reemplazar"/> libera el ViewModel saliente si es
/// <see cref="IDisposable"/>, y después su scope.
/// </para>
/// </remarks>
internal abstract class StoreDeNavegacion
{
	private ObservableObject? _viewModel;
	private IServiceScope? _scope;

	public event Action? ViewModelChanged;

	/// <summary>
	/// El ViewModel activo. Asignarlo directamente equivale a <see cref="Reemplazar"/> sin scope, y es
	/// el camino que usan el barrido de fin de sesión y el cierre del modal.
	/// </summary>
	public ObservableObject ViewModel
	{
		get => _viewModel!;
		set => Reemplazar(value, null);
	}

	/// <summary>Hay una pantalla activa en este store.</summary>
	protected bool TieneViewModel => _viewModel is not null;

	/// <summary>
	/// Pone <paramref name="viewModel"/> como pantalla activa y libera la anterior junto con su scope.
	/// </summary>
	/// <param name="viewModel">El ViewModel entrante, o <see langword="null"/> para dejar el store vacío.</param>
	/// <param name="scope">
	/// El scope del que salió <paramref name="viewModel"/>, cuya vida pasa a ser la de esa pantalla.
	/// <see langword="null"/> cuando el ViewModel no salió de un scope propio.
	/// </param>
	/// <remarks>
	/// <para>
	/// <strong>El orden de las cuatro operaciones no es casual y romperlo tiene síntoma visible.</strong>
	/// Primero se publica el estado nuevo, después se notifica, y sólo al final se libera lo viejo.
	/// Notificar es lo que hace que <c>MainViewModel.OnViewModelChanged</c> dispare
	/// <c>OnPropertyChanged(nameof(ViewModelActual))</c> y que el <c>ContentControl</c> lea el ViewModel
	/// entrante: si se liberara antes de notificar, habría un instante con la vista enlazada a un
	/// ViewModel ya dispuesto y a un <c>DbContext</c> cerrado, y cualquier binding que se evaluara en
	/// ese instante tiraría <c>ObjectDisposedException</c>.
	/// </para>
	/// <para>
	/// Las dos liberaciones van en ese orden —ViewModel y después scope— porque el ViewModel puede
	/// tocar en su <c>Dispose</c> los servicios que le inyectaron, y esos servicios viven en el scope.
	/// </para>
	/// <para>
	/// Reemplazar un ViewModel por sí mismo no libera nada: sería liberar lo que se acaba de poner.
	/// </para>
	/// </remarks>
	public void Reemplazar(ObservableObject? viewModel, IServiceScope? scope)
	{
		if (ReferenceEquals(_viewModel, viewModel) && ReferenceEquals(_scope, scope))
		{
			return;
		}

		var viewModelAnterior = _viewModel;
		var scopeAnterior = _scope;

		_viewModel = viewModel;
		_scope = scope;

		OnViewModelChanged();

		if (!ReferenceEquals(viewModelAnterior, viewModel))
		{
			(viewModelAnterior as IDisposable)?.Dispose();
		}

		if (!ReferenceEquals(scopeAnterior, scope))
		{
			scopeAnterior?.Dispose();
		}
	}

	public void OnViewModelChanged() =>
		ViewModelChanged?.Invoke();
}
