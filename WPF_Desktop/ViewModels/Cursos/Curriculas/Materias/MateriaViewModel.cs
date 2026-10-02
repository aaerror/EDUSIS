using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioMaterias.DTOs.Responses;
using System.ComponentModel.DataAnnotations;
using System;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

internal partial class MateriaViewModel : ObservableValidator
{
	#region Response
	private readonly MateriaResponse _materiaResponse;
	#endregion

	[ObservableProperty]
	private Guid _cursoID = Guid.Empty;

	[ObservableProperty]
	private Guid _curriculaID = Guid.Empty;

	[ObservableProperty]
	private Guid _materiaID = Guid.Empty;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe especificar el nombre de la materia.")]
	[DataType(DataType.Text)]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _descripcion = string.Empty;

	[Required]
	[ObservableProperty]
	private int _horasCatedra;


	public MateriaViewModel(MateriaResponse materiaResponse)
	{
		if (materiaResponse is not null)
		{
			_materiaResponse = materiaResponse;

			CursoID = _materiaResponse.CursoID;
			CurriculaID = _materiaResponse.CurriculaID;
			MateriaID = _materiaResponse.MateriaID;
			Descripcion = _materiaResponse.Descripcion;
			HorasCatedra = _materiaResponse.HorasCatedra;
		}
	}
}