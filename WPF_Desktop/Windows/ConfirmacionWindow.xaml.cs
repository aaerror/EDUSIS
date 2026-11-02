using System.Windows;
using WPF_Desktop.Shared;

namespace WPF_Desktop.Windows;

public partial class ConfirmacionWindow : Window
{
	/// <summary>
	/// Constructor sin parámetros. Lo necesita el humo del tema, que construye todas las
	/// ventanas para detectar claves de recurso inexistentes antes de que las vea un usuario.
	/// </summary>
	public ConfirmacionWindow()
	{
		InitializeComponent();
	}

	internal ConfirmacionWindow(SolicitudDeDialogo solicitud)
		: this()
	{
		DataContext = solicitud;

		// IsDefault no se declara en el XAML: los dos botones de acción están siempre
		// presentes y sólo uno visible, y dos botones marcados como predeterminados dejan
		// indefinido a quién le llega el Enter.
		BotonPeligro.IsDefault = solicitud.EsDestructiva;
		BotonAceptar.IsDefault = !solicitud.EsDestructiva;

		// En una acción destructiva el foco arranca en Cancelar. Quien llega acá con el Enter
		// todavía apretado —porque el botón que abrió el diálogo era el predeterminado de la
		// pantalla— no borra nada por inercia.
		if (solicitud.EsDestructiva && solicitud.TieneCancelar)
		{
			Loaded += (_, _) => BotonCancelar.Focus();
		}
		else
		{
			Loaded += (_, _) => (solicitud.EsDestructiva ? BotonPeligro : BotonAceptar).Focus();
		}
	}

	private void Aceptar_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}

	private void Cancelar_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = false;
	}
}
