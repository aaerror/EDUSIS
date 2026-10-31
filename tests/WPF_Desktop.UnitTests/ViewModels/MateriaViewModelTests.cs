using Core.ServicioMaterias.DTOs.Responses;
using Shouldly;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

public class MateriaViewModelTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_constructor_mapea_cada_campo_del_response()
	{
		var response = new MateriaResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Matematica", 6);

		var viewModel = new MateriaViewModel(response);

		viewModel.CursoID.ShouldBe(response.CursoID);
		viewModel.CurriculaID.ShouldBe(response.CurriculaID);
		viewModel.MateriaID.ShouldBe(response.MateriaID);
		new[] { viewModel.CursoID, viewModel.CurriculaID, viewModel.MateriaID }.Distinct().Count().ShouldBe(3);
		viewModel.Descripcion.ShouldBe("Matematica");
		viewModel.HorasCatedra.ShouldBe(6);
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("CargosOcupados")]
	[InlineData("SituacionRevista")]
	public void El_ViewModel_ya_no_expone_lo_que_la_materia_dejo_de_conocer(string propiedad)
	{
		typeof(MateriaViewModel).GetProperty(propiedad).ShouldBeNull($"MateriaViewModel todavia expone '{propiedad}'");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todo_campo_del_response_tiene_una_propiedad_homonima_del_mismo_tipo()
	{
		foreach (var campo in typeof(MateriaResponse).GetProperties().Where(p => p.Name != "EqualityContract"))
		{
			var propiedad = typeof(MateriaViewModel).GetProperty(campo.Name);

			propiedad.ShouldNotBeNull($"MateriaViewModel no expone '{campo.Name}'");
			propiedad!.PropertyType.ShouldBe(TipoAdmitido(campo.PropertyType, propiedad.PropertyType),
											 $"tipo de '{campo.Name}'");
		}
	}

	/// <summary>
	/// Devuelve el tipo que la propiedad del ViewModel tiene permitido declarar para un campo del
	/// <c>Response</c>: el mismo tipo, o su <see cref="Nullable{T}"/> cuando el ViewModel ensancha
	/// un tipo de valor a anulable.
	/// <para>
	/// El ensanchamiento NO es laxitud: un ViewModel que se bindea a <c>SelectedItem</c> de un
	/// <c>ComboBox</c> necesita representar "nada elegido", y <c>SelectedItem</c> es nulo hasta que
	/// el usuario elige. Es el caso de <c>MateriaViewModel.HorasCatedra</c> (<c>int?</c>) contra
	/// <c>MateriaResponse.HorasCatedra</c> (<c>int</c>), bindeada en
	/// <c>Views/Cursos/Curriculas/Materias/MateriaView.xaml:50</c>. Exigir paridad estricta forzaría
	/// a volver el ViewModel a <c>int</c>, y entonces "sin elegir" y "cero horas" serían el mismo
	/// valor. Ver TEST-15 en <c>docs/todos.md</c>.
	/// </para>
	/// Lo que la prueba sigue atrapando es lo que importa: un campo nuevo del DTO que el ViewModel
	/// no recibe, y un cambio de tipo que no sea ese ensanchamiento. Los tipos exactos de cada
	/// propiedad se fijan aparte, en <c>Los_tipos_de_las_propiedades_son_los_esperados</c>.
	/// </summary>
	private static Type TipoAdmitido(Type tipoDelCampo, Type tipoDeLaPropiedad)
	{
		// Nullable<T> exige que T sea un tipo de valor NO anulable, así que MakeGenericType lanza
		// ArgumentException con un string o con un Guid? — el ensanchamiento sólo se evalúa cuando
		// el campo del Response es un tipo de valor plano.
		if (tipoDelCampo.IsValueType && Nullable.GetUnderlyingType(tipoDelCampo) is null)
		{
			var ensanchado = typeof(Nullable<>).MakeGenericType(tipoDelCampo);
			if (tipoDeLaPropiedad == ensanchado)
			{
				return ensanchado;
			}
		}

		return tipoDelCampo;
	}
}
