using System.Windows.Controls;

namespace WPF_Desktop.Views.Cursos.Curriculas;

public partial class GestionCurriculasView : UserControl
{
	public GestionCurriculasView()
	{
		InitializeComponent();
	}

	private void DataGridCurriculas_Loaded(object sender, System.Windows.RoutedEventArgs e)
	{
		var curriculasDG = sender as DataGrid;

		if (curriculasDG.IsEnabled)
		{
			if (curriculasDG.HasItems)
			{
				var firstItem = curriculasDG.Items[0];
				curriculasDG.SelectedItem = firstItem;
				DataGridRow row = (DataGridRow) curriculasDG.ItemContainerGenerator.ContainerFromItem(firstItem);
				curriculasDG.Focus();
			}
		}
	}

	private void DataGridMaterias_Loaded(object sender, System.Windows.RoutedEventArgs e)
	{
		var materiasDG = sender as DataGrid;

		if (materiasDG.IsEnabled)
		{
			if (materiasDG.HasItems)
			{
				var firstItem = materiasDG.Items[0];
				materiasDG.SelectedItem = firstItem;
				DataGridRow row = (DataGridRow) materiasDG.ItemContainerGenerator.ContainerFromItem(firstItem);
				materiasDG.Focus();
			}
		}
	}
}