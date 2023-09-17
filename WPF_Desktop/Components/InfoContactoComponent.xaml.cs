using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class InfoContactoComponent : UserControl
{
    public static readonly DependencyProperty EstaHabilitadoProperty = DependencyProperty.Register(nameof(EstaHabilitado), typeof(bool), typeof(InfoContactoComponent), new PropertyMetadata(false));
    public static readonly DependencyProperty TelefonoProperty = DependencyProperty.Register(nameof(Telefono), typeof(string), typeof(InfoContactoComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty EmailProperty = DependencyProperty.Register(nameof(Email), typeof(string), typeof(InfoContactoComponent), new PropertyMetadata(string.Empty));


    public InfoContactoComponent()
    {
        InitializeComponent();
    }

    public string Email
    {
        get
        {
            return (string) GetValue(EmailProperty);
        }

        set
        {
            SetValue(EmailProperty, value);
        }
    }

    public string Telefono
    {
        get
        {
            return (string) GetValue(TelefonoProperty);
        }

        set
        {
            SetValue(TelefonoProperty, value);
        }
    }

    public bool EstaHabilitado
    {
        get
        {
            return (bool) GetValue(EstaHabilitadoProperty);
        }

        set
        { 
            SetValue(EstaHabilitadoProperty, value);
        }
    }
}
