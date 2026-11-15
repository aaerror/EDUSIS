using System.Windows.Controls;

namespace WPF_Desktop.Components;

/// <summary>
/// La marca de EDUSIS: el timbre escolar, en vectores y teñido con el <c>Foreground</c> que le pase
/// quien lo use.
/// </summary>
/// <remarks>
/// No declara propiedades propias. El tamaño se pide con <c>Height</c> —el <c>Viewbox</c> mantiene
/// la proporción— y el color con <c>Foreground</c>, que es el mismo par de perillas que tiene
/// cualquier otro control del sistema.
/// </remarks>
public partial class TimbreComponent : UserControl
{
	public TimbreComponent()
	{
		InitializeComponent();
	}
}
