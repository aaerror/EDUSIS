using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes;
using Core.ServicioCursos;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Cursos;

namespace WPF_Desktop.ViewModels.Alumnos;

internal partial class InscripcionAlumnoViewModel : ObservableObject
{
	#region Servicios
	private readonly IServicioCurso _servicioCursos;
	private readonly IServicioCursante _servicioCursantes;
	private readonly IServicioDivision _servicioDivisiones;
	#endregion

	#region Stores
	private readonly LegajoStore _legajoStore;
	private readonly CicloLectivoStore _cicloLectivoStore;
	#endregion

	#region Notifications
	[ObservableProperty]
	private string _message = string.Empty;

	[ObservableProperty]
	private bool _habilitarMessage;
	#endregion

	[NotifyCanExecuteChangedFor(nameof(InscribirCommand))]
	[ObservableProperty]
	private CursoViewModel? _curso;

	[NotifyCanExecuteChangedFor(nameof(InscribirCommand))]
	[ObservableProperty]
	private DivisionViewModel? _division;

	[ObservableProperty]
	private bool _esRecursante;

	public ObservableCollection<CursoViewModel> Cursos { get; } = new();
	public ObservableCollection<DivisionViewModel> Divisiones { get; } = new();

	/// <summary>
	/// Ciclo lectivo de la inscripción: sale de <see cref="CicloLectivoStore"/>, no se tipea acá.
	/// </summary>
	public string Periodo => _cicloLectivoStore.CicloLectivo;

	public IAsyncRelayCommand InscribirCommand { get; }


	public InscripcionAlumnoViewModel(IServicioCurso servicioCursos, IServicioCursante servicioCursantes, IServicioDivision servicioDivisiones, LegajoStore legajoStore, CicloLectivoStore cicloLectivoStore)
	{
		_servicioCursos = servicioCursos;
		_servicioCursantes = servicioCursantes;
		_servicioDivisiones = servicioDivisiones;
		_legajoStore = legajoStore;
		_cicloLectivoStore = cicloLectivoStore;

		InscribirCommand = new AsyncRelayCommand(ExecuteInscribirCommandAsync, CanExecuteInscribirCommand);

		// Las asignaciones a propiedades con [NotifyCanExecuteChangedFor] van DESPUÉS de crear el comando.
		HabilitarMessage = false;

		_ = ActualizarCursosAsync();
	}

	#region Cursos y divisiones
	private async Task ActualizarCursosAsync()
	{
		try
		{
			var cursos = await _servicioCursos.ListarCursosAsync();

			Cursos.Clear();
			foreach (var curso in cursos)
			{
				Cursos.Add(new CursoViewModel(curso));
			}
		}
		catch (Exception ex)
		{
			MostrarMensaje(ex.Message);
		}
	}

	partial void OnCursoChanged(CursoViewModel? value)
	{
		Division = null;
		Divisiones.Clear();

		if (value is not null)
		{
			_ = CargarDivisionesAsync(value.CursoID);
		}
	}

	private async Task CargarDivisionesAsync(Guid cursoID)
	{
		try
		{
			var divisiones = await _servicioDivisiones.ListarDivisionesAsync(new ListarDivisionesRequest(cursoID, Periodo));

			// El usuario pudo cambiar de curso mientras la consulta estaba en vuelo.
			if (Curso is null || Curso.CursoID != cursoID)
			{
				return;
			}

			Divisiones.Clear();
			foreach (var division in divisiones)
			{
				Divisiones.Add(new DivisionViewModel(division));
			}
		}
		catch (Exception ex)
		{
			MostrarMensaje(ex.Message);
		}
	}
	#endregion

	#region InscribirCommand
	private bool CanExecuteInscribirCommand() =>
		Curso is not null && Division is not null;

	private async Task ExecuteInscribirCommandAsync()
	{
		if (Curso is null || Division is null)
		{
			return;
		}

		try
		{
			// El cupo y la inscripción duplicada los valida Core: acá sólo se muestra la excepción.
			var request = new RegistrarCursanteRequest(Curso.CursoID, Division.DivisionID, _legajoStore.PersonaID, Periodo, EsRecursante);
			await _servicioCursantes.InscribirCursanteAsync(request);

			MostrarMensaje("Se ha inscripto el alumno en la división correctamente.");

			EsRecursante = false;
			Curso = null;
		}
		catch (Exception ex)
		{
			MostrarMensaje(ex.Message);
		}
	}
	#endregion

	private void MostrarMensaje(string mensaje)
	{
		Message = mensaje;
		HabilitarMessage = true;
	}
}
