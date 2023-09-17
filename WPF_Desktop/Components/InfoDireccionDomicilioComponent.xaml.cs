using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class InfoDireccionDomicilioComponent : UserControl
{
    public static readonly DependencyProperty CalleProperty = DependencyProperty.Register(nameof(Calle), typeof(string), typeof(InfoDireccionDomicilioComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty AlturaProperty = DependencyProperty.Register(nameof(Altura), typeof(string), typeof(InfoDireccionDomicilioComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ViviendaProperty = DependencyProperty.Register(nameof(Vivienda), typeof(string), typeof(InfoDireccionDomicilioComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ObservacionProperty = DependencyProperty.Register(nameof(Observacion), typeof(string), typeof(InfoDireccionDomicilioComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty EstaHabilitadoProperty = DependencyProperty.Register(nameof(EstaHabilitado), typeof(bool), typeof(InfoDireccionDomicilioComponent), new PropertyMetadata(false));


    public InfoDireccionDomicilioComponent()
    {
        InitializeComponent();
    }

    public string Calle
    {
        get
        {
            return (string) GetValue(CalleProperty);
        }

        set
        {
            SetValue(CalleProperty, value);
        }
    }

    public string Altura
    {
        get
        {
            return (string) GetValue(AlturaProperty);
        }

        set
        {
            SetValue(AlturaProperty, value);
        }
    }

    public string Vivienda
    {
        get
        {
            return (string) GetValue(ViviendaProperty);
        }
        
        set
        {
            SetValue(ViviendaProperty, value);
        }
    }

    public string Observacion
    {
        get
        { 
            return (string) GetValue(ObservacionProperty);
        }
        
        set
        {
            SetValue(ObservacionProperty, value);
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
