using System.Windows.Controls;

namespace WPF_Desktop.Views.Cursos.Divisiones;

public partial class GestionDivisionesView : UserControl
{
	public GestionDivisionesView()
	{
		InitializeComponent();
	}

	private void DataGrid_Divisiones_Loaded(object sender, System.Windows.RoutedEventArgs e)
	{
		var dataGrid = sender as DataGrid;

		if (dataGrid.IsEnabled)
		{
			if (dataGrid.HasItems)
			{
				var firstItem = dataGrid.Items[0];
				dataGrid.SelectedItem = firstItem;
				DataGridRow row = (DataGridRow) dataGrid.ItemContainerGenerator.ContainerFromItem(firstItem);
				dataGrid.Focus();
			}
		}
	}
}