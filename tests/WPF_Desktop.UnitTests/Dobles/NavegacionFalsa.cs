using WPF_Desktop.Navigation;

namespace WPF_Desktop.UnitTests.Dobles;

/// <summary>
/// Doble escrito a mano de <see cref="INavigationService"/>. Cuenta las llamadas a <see cref="Navigate"/> y permite
/// capturar el estado del mundo en el instante exacto de la navegacion (<see cref="AlNavegar"/>), que es lo que hace
/// falta para afirmar "se escribio el store ANTES de navegar".
/// </summary>
internal class NavegacionFalsa : INavigationService
{
	public int Llamadas { get; private set; }

	/// <summary>Se ejecuta dentro de <see cref="Navigate"/>, antes de incrementar el contador.</summary>
	public Action? AlNavegar { get; set; }

	public void Navigate()
	{
		AlNavegar?.Invoke();
		Llamadas++;
	}
}
