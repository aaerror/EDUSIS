using System.Windows.Controls;

namespace WPF_Desktop.Views.Cursos;

public partial class GestionCursosView : UserControl
{
	public GestionCursosView()
	{
		InitializeComponent();
	}

	private void DataGridCursos_Loaded(object sender, System.Windows.RoutedEventArgs e)
	{
		var materiasDG = sender as DataGrid;

		if (materiasDG.IsEnabled)
		{
			if (materiasDG.HasItems)
			{
				var firstItem = materiasDG.Items[0];
				materiasDG.SelectedItem = firstItem;
				DataGridRow row = (DataGridRow)materiasDG.ItemContainerGenerator.ContainerFromItem(firstItem);
				materiasDG.Focus();
			}
		}
	}
}