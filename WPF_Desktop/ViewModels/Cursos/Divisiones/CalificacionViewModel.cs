using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCursantes.DTOs.Responses;
using System.Collections.ObjectModel;
using System;
using WPF_Desktop.Shared;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

internal partial class CalificacionViewModel : ObservableValidator
{
	#region Response
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

	/// <summary>
	/// El tramo del ciclo, con el nombre del miembro de <c>Periodo</c>: es lo que viaja en el
	/// request y lo que selecciona la lista desplegable.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Instancia))]
	private string _periodo = string.Empty;

	/// <summary>Si la calificación es el segundo intento dentro de ese período.</summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Instancia))]
	private bool _esRecuperatorio;

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

	/// <summary>
	/// La instancia como se lee en la grilla: "1° cuatrimestre", "2° cuatrimestre (rec.)".
	/// </summary>
	/// <remarks>
	/// Calculada y no almacenada porque es la lectura de los dos campos que SÍ se editan. Antes era
	/// un <c>string</c> propio con el valor de un enum de dos miembros, y la celda decía
	/// "Recuperatorio" sin poder aclarar de cuál de los cuatro posibles se trataba.
	/// </remarks>
	public string Instancia => Periodos.DecirInstancia(Periodo, EsRecuperatorio);


	public CalificacionViewModel(CalificacionResponse? calificacionResponse)
	{
		if (calificacionResponse is not null)
		{
			_calificacionResponse = calificacionResponse;

			CalificacionID = _calificacionResponse.CalificacionID;
			MateriaID = _calificacionResponse.MateriaID;
			Materia = _calificacionResponse.Materia;
			Fecha = _calificacionResponse.Fecha;
			Periodo = _calificacionResponse.Periodo;
			EsRecuperatorio = _calificacionResponse.EsRecuperatorio;
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
