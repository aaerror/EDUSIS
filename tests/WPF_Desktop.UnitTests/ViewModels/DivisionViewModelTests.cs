using Core.ServicioDivisiones.DTOs.Responses;
using Shouldly;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

public class DivisionViewModelTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_constructor_mapea_cada_campo_del_response()
	{
		var response = new DivisionResponse(Guid.NewGuid(), "A", Guid.NewGuid(), "Perez, Ana", 28);

		var viewModel = new DivisionViewModel(response);

		viewModel.DivisionID.ShouldBe(response.DivisionID);
		viewModel.Descripcion.ShouldBe("A");
		viewModel.PreceptorID.ShouldBe(response.PreceptorID);
		viewModel.Preceptor.ShouldBe("Perez, Ana");
		viewModel.Cursantes.ShouldBe(28);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Una_division_sin_preceptor_deja_PreceptorID_y_Preceptor_en_nulo()
	{
		var response = new DivisionResponse(Guid.NewGuid(), "B", null, null, 0);

		var viewModel = new DivisionViewModel(response);

		viewModel.PreceptorID.ShouldBeNull();
		viewModel.Preceptor.ShouldBeNull();
		viewModel.Cursantes.ShouldBe(0);
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("DocenteID")]
	[InlineData("Docente")]
	[InlineData("Alumnos")]
	public void El_ViewModel_ya_no_expone_los_nombres_anteriores(string propiedad)
	{
		typeof(DivisionViewModel).GetProperty(propiedad).ShouldBeNull($"DivisionViewModel todavia expone '{propiedad}'");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todo_campo_del_response_tiene_una_propiedad_homonima_del_mismo_tipo()
	{
		foreach (var campo in typeof(DivisionResponse).GetProperties().Where(p => p.Name != "EqualityContract"))
		{
			var propiedad = typeof(DivisionViewModel).GetProperty(campo.Name);

			propiedad.ShouldNotBeNull($"DivisionViewModel no expone '{campo.Name}'");
			propiedad!.PropertyType.ShouldBe(campo.PropertyType, $"tipo de '{campo.Name}'");
		}
	}
}
