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
			propiedad!.PropertyType.ShouldBe(campo.PropertyType, $"tipo de '{campo.Name}'");
		}
	}
}
