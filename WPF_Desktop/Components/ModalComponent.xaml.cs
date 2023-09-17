using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class ModalComponent : UserControl
{
    public static readonly DependencyProperty EstaAbiertoProperty = DependencyProperty.Register(nameof(EstaAbierto), typeof(bool), typeof(ModalComponent), new PropertyMetadata(false));


    public ModalComponent()
    {
        InitializeComponent();
    }

    public bool EstaAbierto
    {
        get
        {
            return (bool) GetValue(EstaAbiertoProperty);
        }

        set
        {
            SetValue(EstaAbiertoProperty, value);
        }
    }
}
