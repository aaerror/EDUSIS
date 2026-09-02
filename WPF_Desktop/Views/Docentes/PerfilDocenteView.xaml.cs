using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.Views.Docentes;

public partial class PerfilDocenteView : UserControl
{
	public PerfilDocenteView()
	{
		InitializeComponent();

		//DataContext = App.AppHost.Services.GetRequiredService<PerfilDocenteViewModel>();

		//this.Loaded += (s, e) => ViewModel.IsActive = true;
		//this.Unloaded += (s, e) => ViewModel.IsActive = false;
	}

//	private PerfilDocenteViewModel ViewModel =>
	//	(PerfilDocenteViewModel) DataContext;
}