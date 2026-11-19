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
	/// Decide CUÁNDO se libera el scope de la pantalla saliente cuando otra la reemplaza. Por defecto,
	/// en el acto.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>La vista saliente sobrevive a la notificación.</strong> Al cambiar el contenido, el
	/// <c>ContentPresenter</c> le cambia el <c>DataContext</c> a la vista vieja en el acto, pero la
	/// reemplaza recién en la siguiente pasada de layout. En ese intervalo la vista vieja sigue
	/// procesando eventos: sus bindings dejan de resolverse, una grilla se vacía, dispara
	/// <c>SelectionChanged</c> y un <c>EventTrigger</c> ejecuta un comando del ViewModel SALIENTE.
	/// Con el scope ya liberado, esa consulta tiraba "Cannot access a disposed context instance", y
	/// el diálogo de error se abría en plena pasada de layout y cerraba la aplicación.
	/// </para>
	/// <para>
	/// La aplicación asigna acá un planificador que posterga la liberación hasta que el dispatcher
	/// terminó el layout y el render, es decir, hasta que la vista saliente ya no existe. Las pruebas
	/// dejan el valor por defecto, que conserva la liberación sincrónica.
	/// </para>
	/// <para>
	/// Sólo se posterga el SCOPE, no el ViewModel: el ViewModel saliente se libera en el acto para
	/// que sepa que su pantalla terminó y descarte lo que la vista vieja todavía le pida.
	/// </para>
	/// </remarks>
	public Action<Action> PlanificarLiberacion { get; init; } = liberar => liberar();

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
	/// <para>
	/// El scope saliente pasa por <see cref="PlanificarLiberacion"/> sólo cuando ENTRA otra pantalla.
	/// Vaciar el store (cerrar el modal, el barrido de fin de sesión, <c>App.OnExit</c>) libera en el
	/// acto: no hay traspaso que esperar, y en el apagado un trabajo postergado al dispatcher no
	/// llegaría a correr y dejaría la conexión cerrándose por caída.
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

		if (scopeAnterior is not null && !ReferenceEquals(scopeAnterior, scope))
		{
			if (viewModel is null)
			{
				scopeAnterior.Dispose();
			}
			else
			{
				PlanificarLiberacion(scopeAnterior.Dispose);
			}
		}
	}

	public void OnViewModelChanged() =>
		ViewModelChanged?.Invoke();
}
