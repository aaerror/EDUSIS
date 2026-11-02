using System.Windows.Controls;

namespace WPF_Desktop.Views.Cursos.Curriculas;

public partial class GestionCurriculasView : UserControl
{
	public GestionCurriculasView()
	{
		InitializeComponent();
	}

	/// <summary>
	/// Preselecciona la primera currícula al pintar la grilla, para que la pantalla abra mostrando
	/// algo en lugar de pedir un clic antes de decir nada.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Es comportamiento de presentación pura —qué fila nace elegida— y por eso vive en el
	/// code-behind y no en el ViewModel.
	/// </para>
	/// <para>
	/// SÓLO CORRE EN EL Loaded, que es una limitación conocida: <c>CargarCurriculasAsync</c> reemplaza
	/// la colección entera, así que después de crear una currícula nueva la selección se pierde y hay
	/// que volver a elegir una. Dejarlo acá es deliberado; moverlo exigiría que el ViewModel decida la
	/// selección, que es trabajo de la vista.
	/// </para>
	/// <para>
	/// Se fue <c>DataGridMaterias_Loaded</c>, que era código muerto: las materias dejaron de ser un
	/// <c>DataGrid</c> hace varias pasadas y ningún XAML enganchaba ese manejador. También se fue en
	/// los dos la variable <c>row</c>, que se calculaba y no se usaba.
	/// </para>
	/// </remarks>
	private void DataGridCurriculas_Loaded(object sender, System.Windows.RoutedEventArgs e)
	{
		if (sender is not DataGrid grilla || !grilla.IsEnabled || !grilla.HasItems)
		{
			return;
		}

		grilla.SelectedItem = grilla.Items[0];
		grilla.Focus();
	}
}
