using System.Windows;

namespace WPF_Desktop.Windows;

public partial class StartupWindow : Window
{
	public StartupWindow()
	{
		InitializeComponent();
	}

	/// <summary>
	/// Cierra la ventana de acceso. Si no hay sesión iniciada, <c>App.OnStartup</c> termina la
	/// aplicación; ése es el camino de "no quiero entrar".
	/// </summary>
	private void Cerrar_Click(object sender, RoutedEventArgs e) =>
		Close();
}