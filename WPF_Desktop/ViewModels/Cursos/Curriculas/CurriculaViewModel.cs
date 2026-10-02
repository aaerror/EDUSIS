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

	[ObservableProperty]
	private bool _estaActiva;


	public CurriculaViewModel(CurriculaResponse response)
	{
		if (response is not null)
		{
			_response = response;

			CursoID = _response.CursoID;
			CurriculaID = _response.CurriculaID;
			FechaInicio = _response.FechaInicio;
			FechaFin = _response.FechaFin;
			EstaActiva = !_response.FechaFin.HasValue;
		}
	}
}