using Core.ServicioCursos.DTOs.Responses;
using Domain.Cursos;
using Domain.Shared;
using Shouldly;
using WPF_Desktop.ViewModels.Cursos;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

public class CursoViewModelTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_constructor_mapea_cada_campo_del_response()
	{
		var response = new CursoResponse(Guid.NewGuid(), Grado.Tercero, NivelEducativo.Secundaria);

		var viewModel = new CursoViewModel(response);

		viewModel.CursoID.ShouldBe(response.CursoID);
		viewModel.Grado.ShouldBe("Tercero");
		viewModel.NivelEducativo.ShouldBe("Secundaria");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_response_nulo_no_lanza_y_deja_el_CursoID_vacio()
	{
		CursoViewModel? viewModel = null;

		Should.NotThrow(() => { viewModel = new CursoViewModel(null!); });

		viewModel!.CursoID.ShouldBe(Guid.Empty);
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("Divisiones")]
	[InlineData("Alumnos")]
	public void El_ViewModel_ya_no_expone_los_contadores_eliminados(string propiedad)
	{
		typeof(CursoViewModel).GetProperty(propiedad).ShouldBeNull($"CursoViewModel todavia expone '{propiedad}'");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todo_campo_del_response_tiene_una_propiedad_homonima()
	{
		foreach (var campo in typeof(CursoResponse).GetProperties().Where(p => p.Name != "EqualityContract"))
		{
			typeof(CursoViewModel).GetProperty(campo.Name).ShouldNotBeNull($"CursoViewModel no expone '{campo.Name}'");
		}
	}
}
