using System;
using System.Windows.Controls;
using WPF_Desktop.Shared.Converters;

namespace WPF_Desktop.Views.Docentes.Puestos;

public class BooleanToStringConverter : BooleanConverter<String> { }

public partial class GestionPuestosView : UserControl
{

	public GestionPuestosView()
	{
		InitializeComponent();
	}

	private void DataGrid_Puestos_Loaded(object sender, System.Windows.RoutedEventArgs e)
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