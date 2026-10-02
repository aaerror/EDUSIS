using Core.ServicioCatedras.DTOs.Responses;
using Shouldly;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

public class SituacionRevistaViewModelTests
{
	// Fechas fijas y distintas de hoy: el setter de FechaInicio recalcula EnFunciones contra DateTime.Today,
	// y el valor que manda es el del response.
	private static readonly DateTime _inicio = new(2025, 3, 10);
	private static readonly DateTime _fin = new(2025, 7, 15);

	private static SituacionRevistaResponse CrearSuplente(Guid reemplazaA)
	{
		return new SituacionRevistaResponse(
			SituacionRevistaID: Guid.NewGuid(),
			CatedraID: Guid.NewGuid(),
			DocenteID: Guid.NewGuid(),
			Estado: "Vigente",
			Cargo: "Suplente",
			FechaInicio: _inicio,
			FechaFin: _fin,
			ReemplazaA: reemplazaA,
			EnFunciones: true);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_constructor_mapea_cada_campo_del_response()
	{
		var response = CrearSuplente(Guid.NewGuid());

		var viewModel = new SituacionRevistaViewModel(response);

		viewModel.SituacionRevistaID.ShouldBe(response.SituacionRevistaID);
		viewModel.CatedraID.ShouldBe(response.CatedraID);
		viewModel.DocenteID.ShouldBe(response.DocenteID);
		new[] { viewModel.SituacionRevistaID, viewModel.CatedraID, viewModel.DocenteID }.Distinct().Count().ShouldBe(3);
		viewModel.Estado.ShouldBe("Vigente");
		viewModel.Cargo.ShouldBe("Suplente");
		viewModel.FechaInicio.ShouldBe(_inicio);
		viewModel.FechaFin.ShouldBe(_fin);
		viewModel.ReemplazaA.ShouldBe(response.ReemplazaA);
		viewModel.EnFunciones.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void ReemplazaA_no_se_confunde_con_los_otros_identificadores()
	{
		var response = CrearSuplente(Guid.NewGuid());

		var viewModel = new SituacionRevistaViewModel(response);

		viewModel.ReemplazaA.ShouldNotBeNull();
		viewModel.ReemplazaA.ShouldNotBe(viewModel.SituacionRevistaID);
		viewModel.ReemplazaA.ShouldNotBe(viewModel.DocenteID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_titular_sin_reemplazo_ni_fecha_de_fin_deja_ambos_en_nulo()
	{
		var response = new SituacionRevistaResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Vigente", "Titular", _inicio, null, null, false);

		var viewModel = new SituacionRevistaViewModel(response);

		viewModel.Cargo.ShouldBe("Titular");
		viewModel.FechaFin.ShouldBeNull();
		viewModel.ReemplazaA.ShouldBeNull();
		viewModel.EnFunciones.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void EnFunciones_toma_el_valor_del_response_aunque_la_fecha_de_inicio_no_sea_hoy()
	{
		var response = CrearSuplente(Guid.NewGuid());

		var viewModel = new SituacionRevistaViewModel(response);

		viewModel.FechaInicio.ShouldNotBe(DateTime.Today);
		viewModel.EnFunciones.ShouldBe(response.EnFunciones);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Docente_queda_sin_llenar_porque_lo_completa_el_contenedor()
	{
		var viewModel = new SituacionRevistaViewModel(CrearSuplente(Guid.NewGuid()));

		viewModel.Docente.ShouldBeNullOrEmpty();
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("MateriaID")]
	[InlineData("FechaAlta")]
	[InlineData("FechaBaja")]
	public void El_ViewModel_ya_no_expone_los_nombres_anteriores(string propiedad)
	{
		typeof(SituacionRevistaViewModel).GetProperty(propiedad).ShouldBeNull($"SituacionRevistaViewModel todavia expone '{propiedad}'");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todo_campo_del_response_tiene_una_propiedad_homonima_del_mismo_tipo()
	{
		foreach (var campo in typeof(SituacionRevistaResponse).GetProperties().Where(p => p.Name != "EqualityContract"))
		{
			var propiedad = typeof(SituacionRevistaViewModel).GetProperty(campo.Name);

			propiedad.ShouldNotBeNull($"SituacionRevistaViewModel no expone '{campo.Name}'");
			propiedad!.PropertyType.ShouldBe(campo.PropertyType, $"tipo de '{campo.Name}'");
		}
	}
}
