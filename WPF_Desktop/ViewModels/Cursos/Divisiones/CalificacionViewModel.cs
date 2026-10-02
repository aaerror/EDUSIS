using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCursantes.DTOs.Responses;
using Core.ServicioCursos;
using System.Collections.ObjectModel;
using System;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

internal partial class CalificacionViewModel : ObservableValidator
{
	#region Response
	private readonly IServicioCurso _servicioCursos;
	private readonly CalificacionResponse? _calificacionResponse;
	#endregion

	[ObservableProperty]
	private Guid _calificacionID = Guid.Empty;

	[ObservableProperty]
	private Guid _materiaID = Guid.Empty;

	[ObservableProperty]
	private string _materia = string.Empty;

	[ObservableProperty]
	private bool _rindio;

	[ObservableProperty]
	private DateTime _fecha = DateTime.Today;

	[ObservableProperty]
	private string _instancia = string.Empty;

	[ObservableProperty]
	private double? _nota;

	[ObservableProperty]
	private bool _aprobado;

	[ObservableProperty]
	private string? _observacion;

	[ObservableProperty]
	private bool _mostrarNota;

	[ObservableProperty]
	private ObservableCollection<MateriaViewModel> _materias = new();


	public CalificacionViewModel(IServicioCurso servicioCursos, CalificacionResponse? calificacionResponse)
	{
		_servicioCursos = servicioCursos;

		if (calificacionResponse is not null)
		{
			_calificacionResponse = calificacionResponse;

			CalificacionID = _calificacionResponse.CalificacionID;
			MateriaID = _calificacionResponse.MateriaID;
			Materia = _calificacionResponse.Materia;
			Fecha = _calificacionResponse.Fecha;
			Instancia = _calificacionResponse.Instancia;
			Rindio = _calificacionResponse.Rindio;
			Nota = _calificacionResponse.Nota;
			Aprobado = _calificacionResponse.Aprobado;
			Observacion = _calificacionResponse.Observacion;
		}
	}

	partial void OnRindioChanged(bool value)
	{
		MostrarNota = value;
	}
}
