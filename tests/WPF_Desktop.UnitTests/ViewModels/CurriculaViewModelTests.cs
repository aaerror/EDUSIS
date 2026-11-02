using Core.ServicioCurriculas.DTOs.Responses;
using Shouldly;
using WPF_Desktop.ViewModels.Cursos.Curriculas;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

public class CurriculaViewModelTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_constructor_mapea_cada_campo_del_response()
	{
		var response = new CurriculaResponse(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2020, 3, 1), new DateTime(2024, 12, 20), Vigente: false);

		var viewModel = new CurriculaViewModel(response);

		viewModel.CursoID.ShouldBe(response.CursoID);
		viewModel.CurriculaID.ShouldBe(response.CurriculaID);
		viewModel.CursoID.ShouldNotBe(viewModel.CurriculaID);
		viewModel.FechaInicio.ShouldBe(new DateTime(2020, 3, 1));
		viewModel.FechaFin.ShouldBe(new DateTime(2024, 12, 20));
	}

	/// <summary>
	/// La vigencia la decide el dominio y viaja en el response; el ViewModel ya no la deduce de la
	/// fecha de fin.
	/// </summary>
	/// <remarks>
	/// El caso que lo prueba es justamente el que la deducción vieja erraba: una currícula CON fecha
	/// de fin —futura— que sigue vigente. <c>!FechaFin.HasValue</c> la daba por cerrada y apagaba los
	/// botones de una currícula sobre la que Core sí acepta operar.
	/// </remarks>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_vigencia_sale_del_response_y_no_de_la_fecha_de_fin()
	{
		var conFinFuturo = new CurriculaResponse(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2020, 3, 1), DateTime.Today.AddMonths(2), Vigente: true);

		new CurriculaViewModel(conFinFuturo).EstaActiva.ShouldBeTrue();
		new CurriculaViewModel(conFinFuturo).Vigente.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Una_curricula_sin_FechaFin_esta_activa()
	{
		var response = new CurriculaResponse(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2020, 3, 1), null, Vigente: true);

		var viewModel = new CurriculaViewModel(response);

		viewModel.FechaFin.ShouldBeNull();
		viewModel.EstaActiva.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Una_curricula_cerrada_no_esta_activa()
	{
		var response = new CurriculaResponse(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2020, 3, 1), new DateTime(2024, 12, 20), Vigente: false);

		var viewModel = new CurriculaViewModel(response);

		viewModel.EstaActiva.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void EstaActiva_sigue_existiendo_como_bool()
	{
		// Lo necesita W5.C para elegir la curricula vigente.
		typeof(CurriculaViewModel).GetProperty(nameof(CurriculaViewModel.EstaActiva))!.PropertyType.ShouldBe(typeof(bool));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_ViewModel_ya_no_expone_el_contador_de_materias()
	{
		typeof(CurriculaViewModel).GetProperty("Materias").ShouldBeNull("CurriculaViewModel todavia expone 'Materias'");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todo_campo_del_response_tiene_una_propiedad_homonima_del_mismo_tipo()
	{
		foreach (var campo in typeof(CurriculaResponse).GetProperties().Where(p => p.Name != "EqualityContract"))
		{
			var propiedad = typeof(CurriculaViewModel).GetProperty(campo.Name);

			propiedad.ShouldNotBeNull($"CurriculaViewModel no expone '{campo.Name}'");
			propiedad!.PropertyType.ShouldBe(campo.PropertyType, $"tipo de '{campo.Name}'");
		}
	}
}
