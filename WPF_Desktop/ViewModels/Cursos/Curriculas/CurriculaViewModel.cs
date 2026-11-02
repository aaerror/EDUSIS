using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCurriculas.DTOs.Responses;
using System;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas;

internal partial class CurriculaViewModel : ObservableObject
{
	private readonly CurriculaResponse _response;

	[ObservableProperty]
	private Guid _cursoID;

	[ObservableProperty]
	private Guid _curriculaID;

	[ObservableProperty]
	private DateTime _fechaInicio;

	[ObservableProperty]
	private DateTime? _fechaFin;

	/// <summary>
	/// La currícula está vigente, según el dominio.
	/// </summary>
	/// <remarks>
	/// Viene calculada de <c>Curricula.EstaVigente()</c> y ya no se deduce acá de que
	/// <see cref="FechaFin"/> sea nula. No son lo mismo: una currícula con fecha de fin futura sigue
	/// vigente y la deducción vieja la daba por cerrada, mientras que una con fecha de fin pasada
	/// quedaba marcada como activa sólo si alguien se olvidó de cerrarla. La diferencia importa porque
	/// es el dominio el que después rechaza el alta de materias sobre una currícula no vigente.
	/// </remarks>
	[ObservableProperty]
	private bool _vigente;

	/// <summary>Alias de <see cref="Vigente"/>. Lo consumen las pantallas que eligen la currícula del ciclo.</summary>
	public bool EstaActiva => Vigente;

	partial void OnVigenteChanged(bool value)
	{
		OnPropertyChanged(nameof(EstaActiva));
	}


	public CurriculaViewModel(CurriculaResponse response)
	{
		if (response is not null)
		{
			_response = response;

			CursoID = _response.CursoID;
			CurriculaID = _response.CurriculaID;
			FechaInicio = _response.FechaInicio;
			FechaFin = _response.FechaFin;
			Vigente = _response.Vigente;
		}
	}
}