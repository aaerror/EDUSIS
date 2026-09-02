using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCursos;
using System.Collections.ObjectModel;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Cursos;

namespace WPF_Desktop.ViewModels.Alumnos;

internal partial class InscripcionAlumnoViewModel : ObservableObject
{
	#region Servicios
	private readonly IServicioCurso _servicioCursos;
	#endregion

	#region Notifications
	[ObservableProperty]
	private string _message = string.Empty;

	[ObservableProperty]
	private bool _habilitarMessage;
	#endregion

	private CursoViewModel _curso;
	private ObservableCollection<DivisionViewModel> _divisiones;


	public InscripcionAlumnoViewModel(IServicioCurso servicioCursos)
	{
		_servicioCursos = servicioCursos;

		var cursos = _servicioCursos.ListarCursosAsync();
		//_divisiones = new ObservableCollection<DivisionViewModel>(cursos.Select(x => new CursoViewModel(x)));

		HabilitarMessage = false;
	}

	// TODO: inscripción a medio implementar. Faltan el alumno/división/período de origen,
	// registrar el ViewModel en WPF_DesktopDI y crear su NavigationService.
	// El original no compilaba ("asyn", alumnoID inexistente, RegistrarCursanteRequest pide 4 argumentos).
	/*
	public async void CargarInscripcion()
	{
		var request = new RegistrarCursanteRequest(alumnoID);
	}
	*/

	#region Properties
	public CursoViewModel Curso
	{
		get
		{
			return _curso;
		}

		set
		{
			_curso = value;
			OnPropertyChanged(nameof(Curso));
		}
	}

	public ObservableCollection<DivisionViewModel> Divisiones
	{
		get
		{
			return _divisiones;
		}
	}
	#endregion
}
