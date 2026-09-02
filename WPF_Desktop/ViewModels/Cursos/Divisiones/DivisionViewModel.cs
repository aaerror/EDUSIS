using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCursos.DTOs.Responses;
using System;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

internal partial class DivisionViewModel : ObservableObject
{
	#region Response
	private readonly DivisionResponse _divisionResponse;
	#endregion

	[ObservableProperty]
	private Guid _divisionID = Guid.Empty;

	[ObservableProperty]
	private string _descripcion;

	[ObservableProperty]
	private Guid? _docenteID = Guid.Empty;

	[ObservableProperty]
	private string? _docente;

	[ObservableProperty]
	private int _alumnos;


	public DivisionViewModel(DivisionResponse divisionResponse)
	{
		if (divisionResponse is not null)
		{
			_divisionResponse = divisionResponse;

			DivisionID = _divisionResponse.DivisionID;
			Descripcion = _divisionResponse.Descripcion;
			DocenteID = _divisionResponse.DocenteID;
			Docente = _divisionResponse.Docente;
			Alumnos = _divisionResponse.Alumnos;
		}
	}
}