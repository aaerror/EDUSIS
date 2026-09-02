using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows;
using System;
using WPF_Desktop.Navigation;

namespace WPF_Desktop.ViewModels.Cursos;

internal partial class RegistrarCursosViewModel : ObservableValidator
{
	private readonly IServicioCurso _servicioCursos;
	private readonly INavigationService _gestionCursosNavigationService;

	[Required(AllowEmptyStrings=false)]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _grado = string.Empty;

	[Required(AllowEmptyStrings=false)]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _nivelEducativo = string.Empty;


	public RegistrarCursosViewModel(IServicioCurso servicioCursos, INavigationService gestionCursosNavigationService)
	{
		_servicioCursos = servicioCursos;
		_gestionCursosNavigationService = gestionCursosNavigationService;

		//RegistrarCommandAsync = new AsyncRelayCommand(ExecuteRegistrarCommandAsync, CanExecuteRegistrarCommandAsync);
		/*NavigationCommand = new RelayCommand(() =>
			{
			});*/
	}

	#region NavigationCommand
	[RelayCommand]
	private void Navigation() =>
		_gestionCursosNavigationService.Navigate();
	#endregion

	#region RegistrarCommand
	private bool CanExecuteRegistrarCommand() =>
		!HasErrors;

	[RelayCommand(AllowConcurrentExecutions=true, CanExecute=nameof(CanExecuteRegistrarCommand))]
	private async Task Registrar()
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		messageBoxText = $"Se van a registrar los siguientes datos del nuevo curso:\n" +
						 $"Grado: { Grado }\n" +
						 $"Nivel Educativo: { NivelEducativo }\n\n" +
						 $"¿Desea continuar?";
		caption = "Registrar Curso";

		result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (result is MessageBoxResult.Yes)
		{
			try
			{
				await _servicioCursos.RegistrarCurso(new RegistrarCursoRequest(Grado, NivelEducativo));

				MessageBox.Show("Datos guardados correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error al registrar un nuevo curso. { ex.Message }", "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
	#endregion
}