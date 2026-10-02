using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioDivisiones.DTOs.Responses;
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
	private Guid? _preceptorID;

	[ObservableProperty]
	private string? _preceptor;

	[ObservableProperty]
	private int _cursantes;


	public DivisionViewModel(DivisionResponse divisionResponse)
	{
		if (divisionResponse is not null)
		{
			_divisionResponse = divisionResponse;

			DivisionID = _divisionResponse.DivisionID;
			Descripcion = _divisionResponse.Descripcion;
			PreceptorID = _divisionResponse.PreceptorID;
			Preceptor = _divisionResponse.Preceptor;
			Cursantes = _divisionResponse.Cursantes;
		}
	}
}