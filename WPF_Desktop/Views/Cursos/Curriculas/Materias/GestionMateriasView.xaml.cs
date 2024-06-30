using System.Windows.Controls;

namespace WPF_Desktop.Views.Cursos.Curriculas.Materias;

public partial class GestionMateriasView : UserControl
{
    public GestionMateriasView()
    {
        InitializeComponent();
    }

    private void DataGridMaterias_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        var dataGridMaterias = sender as DataGrid;
        
        if (dataGridMaterias.HasItems)
        {
            var firstItem = dataGridMaterias.Items[0];
            dataGridMaterias.SelectedItem = firstItem;
            DataGridRow row = (DataGridRow) dataGridMaterias.ItemContainerGenerator.ContainerFromItem(firstItem);
            dataGridMaterias.Focus();
        }
    }
}