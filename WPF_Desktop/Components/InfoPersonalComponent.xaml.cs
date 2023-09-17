using System;
using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class InfoPersonalComponent : UserControl
{
    public static readonly DependencyProperty ApellidoProperty = DependencyProperty.Register(nameof(Apellido), typeof(string), typeof(InfoPersonalComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty NombreProperty = DependencyProperty.Register(nameof(Nombre), typeof(string), typeof(InfoPersonalComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty DocumentoProperty = DependencyProperty.Register(nameof(Documento), typeof(string), typeof(InfoPersonalComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty SexoProperty = DependencyProperty.Register(nameof(Sexo), typeof(string), typeof(InfoPersonalComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty FechaNacimientoProperty = DependencyProperty.Register(nameof(FechaNacimiento), typeof(string), typeof(InfoPersonalComponent), new PropertyMetadata(DateTime.Now.ToString("D")));
    public static readonly DependencyProperty NacionalidadProperty = DependencyProperty.Register(nameof(Nacionalidad), typeof(string),typeof(InfoPersonalComponent), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty EstaHabilitadoProperty = DependencyProperty.Register(nameof(EstaHabilitado), typeof(bool), typeof(InfoPersonalComponent), new PropertyMetadata(false));


    public InfoPersonalComponent()
    {
        InitializeComponent();
    }

    public string Apellido
    {
        get
        {
            return (string) GetValue(ApellidoProperty);
        }

        set
        {
            SetValue(ApellidoProperty, value);
        }
    }

    public string Nombre
    {
        get
        {
            return (string) GetValue(NombreProperty);
        }

        set
        {
            SetValue(NombreProperty, value);
        }
    }

    public string Documento
    {
        get
        {
            return (string) GetValue(DocumentoProperty);
        }

        set
        {
            SetValue(DocumentoProperty, value);
        }
    }

    public string Sexo
    {
        get
        {
            return (string) GetValue(SexoProperty);
        }

        set
        {
            SetValue(SexoProperty, value);
        }
    }

    public string FechaNacimiento
    {
        get
        {
            return (string) GetValue(FechaNacimientoProperty);
        }

        set
        {
            SetValue(FechaNacimientoProperty, value);
        }
    }

    public string Nacionalidad
    {
        get
        {
            return (string) GetValue(NacionalidadProperty);
        }

        set
        {
            SetValue(NacionalidadProperty, value);
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
