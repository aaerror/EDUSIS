using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCursos.DTOs.Responses;
using System;

namespace WPF_Desktop.ViewModels.Cursos;

internal partial class CursoViewModel : ObservableObject
{
	#region Response
	private readonly CursoResponse _cursoResponse;
	#endregion

	[ObservableProperty]
	private Guid _cursoID = Guid.Empty;

	[ObservableProperty]
	private string _grado;

	[ObservableProperty]
	private string _nivelEducativo;

	[ObservableProperty]
	private string _divisiones;

	[ObservableProperty]
	private string _alumnos;


	public CursoViewModel(CursoResponse cursoResponses)
	{
		if (cursoResponses is not null)
		{
			_cursoResponse = cursoResponses;

			CursoID = _cursoResponse.CursoID;
			Grado = _cursoResponse.Grado.ToString();
			NivelEducativo = _cursoResponse.NivelEducativo.ToString();
			Divisiones = _cursoResponse.Divisiones.ToString();
			Alumnos = _cursoResponse.Alumnos.ToString();
		}
	}
}