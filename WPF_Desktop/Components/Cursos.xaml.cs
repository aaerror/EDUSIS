using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class Cursos : UserControl
{
    private ObservableCollection<Cursos> _cursos;

    // El valor por omisión era 0 —un int— para una propiedad de tipo ObservableCollection:
    // WPF valida el tipo al registrar la DependencyProperty, así que construir este control
    // tiraba "El tipo de valor predeterminado no coincide con el tipo de propiedad". Hoy no
    // lo instancia ninguna vista, por eso nunca saltó.
    public static readonly DependencyProperty ListaCursosProperty = DependencyProperty.Register(nameof(ListaCursos), typeof(ObservableCollection<Cursos>), typeof(Cursos), new PropertyMetadata(null));

    public Cursos()
    {
        InitializeComponent();
        _cursos = ListaCursos;
    }

    public ObservableCollection<Cursos> ListaCursos
    {
        get
        {
            return (ObservableCollection<Cursos>) GetValue(ListaCursosProperty);
        }

        set
        {
            SetValue(ListaCursosProperty, value);
        }
    }

    



    


}
