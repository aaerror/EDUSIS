using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class InfoUbicacionDomicilioComponent : UserControl
{
    public static readonly DependencyProperty LocalidadProperty = DependencyProperty.Register(nameof(Localidad), typeof(string), typeof(InfoUbicacionDomicilioComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ProvinciaProperty = DependencyProperty.Register(nameof(Provincia), typeof(string), typeof(InfoUbicacionDomicilioComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty PaisProperty = DependencyProperty.Register(nameof(Pais), typeof(string), typeof(InfoUbicacionDomicilioComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty EstaHabilitadoProperty = DependencyProperty.Register("EstaHabilitado", typeof(bool), typeof(InfoUbicacionDomicilioComponent), new PropertyMetadata(false));


    public InfoUbicacionDomicilioComponent()
    {
        InitializeComponent();
    }

    public string Localidad
    {
        get
        {
            return (string)GetValue(LocalidadProperty);
        }
        
        set
        {
            SetValue(LocalidadProperty, value);
        }
    }

    public string Provincia
    {
        get
        {
            return (string) GetValue(ProvinciaProperty);
        }
        
        set
        {
            SetValue(ProvinciaProperty, value);
        }
    }

    public string Pais
    {
        get
        {
            return (string)GetValue(PaisProperty);
        }

        set
        {
            SetValue(PaisProperty, value);
        }
    }

    public bool EstaHabilitado
    {
        get
        {
            return (bool)GetValue(EstaHabilitadoProperty);
        }

        set
        {
            SetValue(EstaHabilitadoProperty, value);
        }
    }
}