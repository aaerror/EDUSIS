using System;
using System.Windows;
using System.Windows.Threading;

namespace WPF_Desktop.Navigation;

/// <summary>
/// Posterga la liberación del scope de una pantalla saliente hasta que WPF terminó de
/// reemplazar su vista.
/// </summary>
/// <remarks>
/// <para>
/// Es el planificador que <c>WPF_DesktopDI</c> le asigna a los tres <c>*NavigationStore</c> en
/// <c>StoreDeNavegacion.PlanificarLiberacion</c>. La prioridad <see cref="DispatcherPriority.ContextIdle"/>
/// está por debajo de <c>Render</c> y de <c>Loaded</c>: el trabajo corre cuando el dispatcher ya
/// hizo el layout que retira la vista vieja, así que ningún evento de esa vista puede alcanzar un
/// <c>DbContext</c> liberado.
/// </para>
/// <para>
/// Sin dispatcher —no hay <see cref="Application"/>— libera en el acto: no hay vista que esperar.
/// </para>
/// </remarks>
internal static class LiberacionTrasElTraspaso
{
	public static void Planificar(Action liberar)
	{
		var dispatcher = Application.Current?.Dispatcher;

		if (dispatcher is null || dispatcher.HasShutdownStarted)
		{
			liberar();

			return;
		}

		dispatcher.BeginInvoke(liberar, DispatcherPriority.ContextIdle);
	}
}
