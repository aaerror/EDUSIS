using System.Windows.Controls;
using System.Windows;

namespace WPF_Desktop.Views.Usuarios;

public partial class LoginUsuarioView : UserControl
{
    public LoginUsuarioView()
    {
        InitializeComponent();

        Loaded += AlCargar;
    }

    /// <summary>
    /// Deja el cursor en el campo de usuario apenas aparece la pantalla.
    /// </summary>
    /// <remarks>
    /// <para>
    /// La ventana de acceso es lo primero que se ve de la aplicación y abría sin foco en ningún
    /// lado: había que ir a buscar el campo con el mouse antes de poder escribir, y la tecla Enter
    /// —que el botón "Ingresar" ya escucha con IsDefault— no servía de nada hasta entonces.
    /// </para>
    /// <para>
    /// Va en el code-behind y no en FocusManager.FocusedElement de la ventana porque la vista entra
    /// a un ContentControl por navegación: cuando la ventana se construye, este campo todavía no
    /// existe. Es foco, no lógica de presentación, así que no le corresponde al ViewModel.
    /// </para>
    /// </remarks>
    private void AlCargar(object remitente, RoutedEventArgs argumentos) =>
        CampoUsuario.Focus();
}
