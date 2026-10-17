using System.Windows;
using System;
using WPF_Desktop.Shared.Converters;

namespace WPF_Desktop.Windows;

public partial class MainWindow : Window
{
	public class BooleanToStringConverter : BooleanConverter<String> { }

	public MainWindow()
	{
		InitializeComponent();

		StateChanged += AlCambiarEstado;
	}

	#region Marco de ventana
	private void Minimizar_Click(object sender, RoutedEventArgs e) =>
		WindowState = WindowState.Minimized;

	private void Maximizar_Click(object sender, RoutedEventArgs e) =>
		WindowState = WindowState is WindowState.Maximized
			? WindowState.Normal
			: WindowState.Maximized;

	private void Cerrar_Click(object sender, RoutedEventArgs e) =>
		Close();

	/// <summary>
	/// Ajusta el botón de maximizar y compensa el borde de redimensionado al maximizar.
	/// </summary>
	/// <remarks>
	/// Con <c>WindowChrome</c>, el borde de redimensionado es parte de la ventana aunque no se
	/// dibuje. Maximizada, la ventana se extiende por debajo de ese borde y el contenido queda
	/// recortado contra los cuatro lados de la pantalla. Compensarlo con
	/// <c>BorderThickness</c> es más barato que interceptar <c>WM_GETMINMAXINFO</c>, y además
	/// no hay que recalcular nada al cambiar de monitor.
	/// </remarks>
	private void AlCambiarEstado(object? sender, EventArgs e)
	{
		bool maximizada = WindowState is WindowState.Maximized;

		// E923 es el glifo de "restaurar" y E922 el de "maximizar", en Segoe MDL2 Assets.
		BotonMaximizar.Content = maximizada ? "" : "";
		BotonMaximizar.ToolTip = maximizada ? "Restaurar" : "Maximizar";

		BorderThickness = maximizada
			? new Thickness(SystemParameters.WindowResizeBorderThickness.Left + 1)
			: new Thickness(0);
	}
	#endregion
}
