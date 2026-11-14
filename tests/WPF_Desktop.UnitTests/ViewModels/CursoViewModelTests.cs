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

	/// <remarks>
	/// Las dos propiedades tipadas existen porque <c>Domain.Cursos.EdadEscolar</c> necesita los
	/// valores y no las etiquetas, y la alternativa sería un <c>Enum.Parse</c> sobre el texto de
	/// pantalla. Esta prueba fija que salgan del response y no de parsear las cadenas: si alguien
	/// las recableara a <c>Enum.Parse(Grado)</c>, seguiría pasando — por eso la que importa es la
	/// de abajo, con el response nulo, donde un parseo de cadena devolvería el default del enum
	/// (<c>Grado.Primero</c>, que vale 1) en lugar de nulo.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Expone_el_grado_y_el_nivel_como_valores_del_dominio()
	{
		var response = new CursoResponse(Guid.NewGuid(), Grado.Tercero, NivelEducativo.Secundaria);

		var viewModel = new CursoViewModel(response);

		viewModel.ValorGrado.ShouldBe(Grado.Tercero);
		viewModel.ValorNivelEducativo.ShouldBe(NivelEducativo.Secundaria);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Con_un_response_nulo_los_valores_del_dominio_quedan_en_nulo_y_no_en_el_default_del_enum()
	{
		var viewModel = new CursoViewModel(null!);

		viewModel.ValorGrado.ShouldBeNull();
		viewModel.ValorNivelEducativo.ShouldBeNull();
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
