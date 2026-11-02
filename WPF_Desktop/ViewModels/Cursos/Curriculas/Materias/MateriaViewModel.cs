using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioMaterias.DTOs.Responses;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

	/// <remarks>
	/// LE FALTABA <c>[NotifyDataErrorInfo]</c>, así que la regla estaba escrita pero NUNCA se
	/// evaluaba: <c>ObservableValidator</c> sólo valida en el setter las propiedades que lo llevan.
	/// Una materia sin carga horaria pasaba el <c>HasErrors</c> del ViewModel y moría más abajo, en
	/// <c>Materia.EditarCargaHoraria</c>, con <c>CargaHorariaInvalidaException</c> sobre el cero que
	/// la pantalla manda cuando el valor es nulo.
	///
	/// El <c>[Range]</c> va además del <c>[Required]</c> porque el nulo y el cero son dos faltas
	/// distintas y el dominio rechaza las dos.
	/// </remarks>
	[Required(ErrorMessage="Se debe especificar la carga horaria de la materia.")]
	[Range(1, int.MaxValue, ErrorMessage="La carga horaria tiene que ser de al menos una hora cátedra.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private int? _horasCatedra;

	/// <summary>
	/// Las cargas horarias que el combo de edición puede ofrecer para ESTA materia.
	/// </summary>
	/// <remarks>
	/// EL COMBO BORRABA LA CARGA HORARIA DE LA MATERIA. Tenía una lista fija de 1 a 5 y el
	/// <c>SelectedItem</c> en <c>TwoWay</c>: si la materia valía algo fuera de esa lista, WPF no
	/// encontraba el ítem y escribía <c>null</c> de vuelta en la propiedad. Con eso la materia pasaba
	/// a estar inválida sola, apenas se abría el editor, y el botón de guardar —cuya visibilidad
	/// cuelga de su <c>IsEnabled</c>— desaparecía sin decir por qué.
	///
	/// Arranca en 1..5 incluyendo SIEMPRE el valor actual, así que ninguna materia puede quedar fuera
	/// de su propio combo. El contenedor la ajusta al cupo real de la currícula cuando lo conoce.
	/// </remarks>
	[ObservableProperty]
	private ObservableCollection<int> _opcionesCargaHoraria = new();

	/// <summary>
	/// Recalcula las opciones del combo hasta <paramref name="maximo"/>, conservando el valor actual.
	/// </summary>
	public void OfrecerCargasHorariaHasta(int maximo)
	{
		var tope = Math.Max(1, maximo);

		if (HorasCatedra.HasValue && HorasCatedra.Value > tope)
		{
			tope = HorasCatedra.Value;
		}

		OpcionesCargaHoraria = new ObservableCollection<int>(Enumerable.Range(1, tope));
	}


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

		// Nace con un combo que al menos contiene su propio valor, aunque nadie le ajuste el cupo.
		OfrecerCargasHorariaHasta(5);
	}
}