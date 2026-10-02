using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCatedras.DTOs.Responses;
using System;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.Catedras;

internal partial class CatedraViewModel : ObservableObject
{
	#region Response
	private readonly CatedraResponse _catedraResponse;
	#endregion

	[ObservableProperty]
	private Guid _catedraID = Guid.Empty;

	[ObservableProperty]
	private Guid _materiaID = Guid.Empty;

	[ObservableProperty]
	private Guid _divisionID = Guid.Empty;

	[ObservableProperty]
	private int _cargaHoraria;

	[ObservableProperty]
	private int _horasAsignadas;

	[ObservableProperty]
	private int _horasSinAsignar;

	[ObservableProperty]
	private Guid? _docenteEnFuncionesID;

	[ObservableProperty]
	private string _division = string.Empty;


	public CatedraViewModel(CatedraResponse catedraResponse)
	{
		if (catedraResponse is not null)
		{
			_catedraResponse = catedraResponse;

			CatedraID = _catedraResponse.CatedraID;
			MateriaID = _catedraResponse.MateriaID;
			DivisionID = _catedraResponse.DivisionID;
			CargaHoraria = _catedraResponse.CargaHoraria;
			HorasAsignadas = _catedraResponse.HorasAsignadas;
			HorasSinAsignar = _catedraResponse.HorasSinAsignar;
			DocenteEnFuncionesID = _catedraResponse.DocenteEnFuncionesID;
		}
	}
}
