using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace WPF_Desktop.Components;

public partial class Cursos : UserControl
{
    private ObservableCollection<Cursos> _cursos;

    public static readonly DependencyProperty ListaCursosProperty = DependencyProperty.Register(nameof(ListaCursos), typeof(ObservableCollection<Cursos>), typeof(Cursos), new PropertyMetadata(0));

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
