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
	private int? _cargaHoraria;

	[ObservableProperty]
	private int _horasAsignadas;

	[ObservableProperty]
	private int _horasSinAsignar;

	[ObservableProperty]
	private Guid? _docenteEnFuncionesID;

	[ObservableProperty]
	private string _division = string.Empty;

	[ObservableProperty]
	private string _materia = string.Empty;

	/// <summary>Nombre de quien está frente al curso, o <c>null</c> si nadie.</summary>
	[ObservableProperty]
	private string? _docenteEnFunciones;


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

			// Los nombres los resuelve Core: antes la letra la completaba la pantalla cruzando el
			// listado de divisiones.
			Materia = _catedraResponse.Materia;
			Division = _catedraResponse.Division;
			DocenteEnFunciones = _catedraResponse.DocenteEnFunciones;
		}
	}
}
