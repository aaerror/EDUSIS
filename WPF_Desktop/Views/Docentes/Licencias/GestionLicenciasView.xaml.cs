using System.Windows.Controls;

namespace WPF_Desktop.Views.Docentes.Licencias;

public partial class GestionLicenciasView : UserControl
{
	public GestionLicenciasView()
	{
		InitializeComponent();
	}

	private void DataGridLicencias_Loaded(object sender, System.Windows.RoutedEventArgs e)
	{
		var licenciasDG = sender as DataGrid;

		if (licenciasDG.HasItems)
		{
			var firstItem = licenciasDG.Items[0];
			licenciasDG.SelectedItem = firstItem;
			DataGridRow row = (DataGridRow) licenciasDG.ItemContainerGenerator.ContainerFromItem(firstItem);
			licenciasDG.Focus();
		}
	}
}