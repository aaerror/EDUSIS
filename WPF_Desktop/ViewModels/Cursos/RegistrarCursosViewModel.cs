using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

internal partial class RegistrarCursosViewModel : ObservableValidator, IUbicacion
{
	private readonly IServicioCurso _servicioCursos;
	private readonly IDialogService _dialogService;
	private readonly INavigationService _gestionCursosNavigationService;

	[Required(AllowEmptyStrings=false)]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _grado = string.Empty;

	[Required(AllowEmptyStrings=false)]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _nivelEducativo = string.Empty;

	public IReadOnlyList<Migaja> Ruta { get; }


	public RegistrarCursosViewModel(IServicioCurso servicioCursos,
									IDialogService dialogService,
									INavigationService gestionCursosNavigationService)
	{
		_servicioCursos = servicioCursos;
		_dialogService = dialogService;
		_gestionCursosNavigationService = gestionCursosNavigationService;

		Ruta = new[]
		{
			Migaja.Hacia("CURSOS", NavigationCommand),
			Migaja.Actual("ALTA")
		};
	}

	#region NavigationCommand
	[RelayCommand]
	private void Navigation() =>
		_gestionCursosNavigationService.Navigate();
	#endregion

	#region RegistrarCommand
	private bool CanExecuteRegistrarCommand() =>
		!HasErrors;

	/// <summary>
	/// Da de alta el curso.
	/// </summary>
	/// <remarks>
	/// Sin el "¿Desea continuar?" que repetía en un cartel los dos combos que el usuario tiene
	/// delante. Lo que sí hace ahora es decir qué curso quedó creado y volver al listado, que
	/// es a dónde va el usuario después de un alta: antes se quedaba en el formulario vacío sin
	/// saber si el curso estaba o no.
	/// </remarks>
	[RelayCommand(AllowConcurrentExecutions=true, CanExecute=nameof(CanExecuteRegistrarCommand))]
	private async Task Registrar()
	{
		try
		{
			string curso = $"{ Grados.Decir(Grado) } ({ NivelEducativo })";

			await _servicioCursos.RegistrarCurso(new RegistrarCursoRequest(Grado, NivelEducativo));

			_dialogService.MostrarInformacion($"{ curso } ya figura en el listado.", "Curso registrado");
			_gestionCursosNavigationService.Navigate();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo registrar el curso");
		}
	}
	#endregion
}
