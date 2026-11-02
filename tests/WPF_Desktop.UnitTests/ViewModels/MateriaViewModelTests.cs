using Core.ServicioMaterias.DTOs.Responses;
using Shouldly;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

public class MateriaViewModelTests
{
	/// <summary>
	/// El combo de edición nunca puede dejar afuera la carga que la materia ya tiene.
	/// </summary>
	/// <remarks>
	/// ES LA GUARDA DE UN DEFECTO QUE BORRABA DATOS. El combo tenía una lista fija de 1 a 5 y el
	/// <c>SelectedItem</c> en <c>TwoWay</c>: una materia de 6 horas no encontraba su ítem, WPF le
	/// escribía <c>null</c> de vuelta, la materia quedaba inválida apenas se abría el editor y
	/// "Guardar materia" —cuya visibilidad cuelga del <c>IsEnabled</c>— desaparecía sin decir por
	/// qué. Seis horas no es un caso raro: el response que usa el resto de este archivo ya traía esa
	/// carga.
	/// </remarks>
	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(6)]
	[InlineData(8)]
	[InlineData(1)]
	public void Las_opciones_de_carga_horaria_contienen_siempre_la_de_la_materia(int horas)
	{
		var viewModel = new MateriaViewModel(new MateriaResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Matematica", horas));

		viewModel.OpcionesCargaHoraria.ShouldContain(horas);

		// Y tampoco se pierde cuando el contenedor le aprieta el tope al cupo de la currícula.
		viewModel.OfrecerCargasHorariaHasta(2);

		viewModel.OpcionesCargaHoraria.ShouldContain(horas);
		viewModel.HorasCatedra.ShouldBe(horas);
	}

	/// <summary>Con cupo de sobra el combo llega hasta donde le digan, empezando en 1.</summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Las_opciones_de_carga_horaria_van_de_uno_hasta_el_tope_recibido()
	{
		var viewModel = new MateriaViewModel(new MateriaResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Matematica", 2));

		viewModel.OfrecerCargasHorariaHasta(4);

		viewModel.OpcionesCargaHoraria.ShouldBe(new[] { 1, 2, 3, 4 });
	}

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
