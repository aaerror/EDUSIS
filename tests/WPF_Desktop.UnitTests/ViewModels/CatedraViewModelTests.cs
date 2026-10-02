using Core.ServicioCatedras.DTOs.Responses;
using Shouldly;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.Catedras;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

public class CatedraViewModelTests
{
	private static CatedraResponse CrearResponse(Guid? docenteEnFuncionesID)
	{
		return new CatedraResponse(
			CatedraID: Guid.NewGuid(),
			MateriaID: Guid.NewGuid(),
			DivisionID: Guid.NewGuid(),
			CargaHoraria: 6,
			HorasAsignadas: 4,
			HorasSinAsignar: 2,
			DocenteEnFuncionesID: docenteEnFuncionesID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_constructor_mapea_cada_campo_del_response()
	{
		var response = CrearResponse(Guid.NewGuid());

		var viewModel = new CatedraViewModel(response);

		viewModel.CatedraID.ShouldBe(response.CatedraID);
		viewModel.MateriaID.ShouldBe(response.MateriaID);
		viewModel.DivisionID.ShouldBe(response.DivisionID);
		viewModel.CargaHoraria.ShouldBe(6);
		viewModel.HorasAsignadas.ShouldBe(4);
		viewModel.HorasSinAsignar.ShouldBe(2);
		viewModel.DocenteEnFuncionesID.ShouldBe(response.DocenteEnFuncionesID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_mapeo_no_cruza_los_tres_Guid_de_identidad()
	{
		var response = CrearResponse(Guid.NewGuid());

		var viewModel = new CatedraViewModel(response);

		new[] { viewModel.CatedraID, viewModel.MateriaID, viewModel.DivisionID, viewModel.DocenteEnFuncionesID!.Value }
			.Distinct().Count().ShouldBe(4);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Una_catedra_sin_docente_en_funciones_deja_el_Guid_nulo()
	{
		var viewModel = new CatedraViewModel(CrearResponse(null));

		viewModel.DocenteEnFuncionesID.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Los_tipos_de_las_propiedades_son_los_esperados()
	{
		var tipo = typeof(CatedraViewModel);

		tipo.GetProperty(nameof(CatedraViewModel.CatedraID))!.PropertyType.ShouldBe(typeof(Guid));
		tipo.GetProperty(nameof(CatedraViewModel.MateriaID))!.PropertyType.ShouldBe(typeof(Guid));
		tipo.GetProperty(nameof(CatedraViewModel.DivisionID))!.PropertyType.ShouldBe(typeof(Guid));
		tipo.GetProperty(nameof(CatedraViewModel.CargaHoraria))!.PropertyType.ShouldBe(typeof(int));
		tipo.GetProperty(nameof(CatedraViewModel.HorasAsignadas))!.PropertyType.ShouldBe(typeof(int));
		tipo.GetProperty(nameof(CatedraViewModel.HorasSinAsignar))!.PropertyType.ShouldBe(typeof(int));
		tipo.GetProperty(nameof(CatedraViewModel.DocenteEnFuncionesID))!.PropertyType.ShouldBe(typeof(Guid?));
		tipo.GetProperty(nameof(CatedraViewModel.Division))!.PropertyType.ShouldBe(typeof(string));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todo_campo_del_response_tiene_una_propiedad_homonima_del_mismo_tipo()
	{
		// Si el DTO de Core gana un campo nuevo y el ViewModel no lo recibe, esta prueba falla.
		foreach (var campo in typeof(CatedraResponse).GetProperties().Where(p => p.Name != "EqualityContract"))
		{
			var propiedad = typeof(CatedraViewModel).GetProperty(campo.Name);

			propiedad.ShouldNotBeNull($"CatedraViewModel no expone '{campo.Name}'");
			propiedad!.PropertyType.ShouldBe(campo.PropertyType, $"tipo de '{campo.Name}'");
		}
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Division_queda_vacia_hasta_que_la_completa_el_contenedor()
	{
		var viewModel = new CatedraViewModel(CrearResponse(null));

		viewModel.Division.ShouldBe(string.Empty);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_Division_notifica_el_cambio_de_propiedad()
	{
		var viewModel = new CatedraViewModel(CrearResponse(null));
		var notificadas = new List<string?>();
		viewModel.PropertyChanged += (_, e) => notificadas.Add(e.PropertyName);

		viewModel.Division = "1A";

		viewModel.Division.ShouldBe("1A");
		notificadas.ShouldContain(nameof(CatedraViewModel.Division));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_response_nulo_no_lanza_y_deja_los_valores_por_defecto()
	{
		CatedraViewModel? viewModel = null;

		Should.NotThrow(() => viewModel = new CatedraViewModel(null!));

		viewModel!.CatedraID.ShouldBe(Guid.Empty);
		viewModel.MateriaID.ShouldBe(Guid.Empty);
		viewModel.DivisionID.ShouldBe(Guid.Empty);
		viewModel.CargaHoraria.ShouldBe(0);
		viewModel.DocenteEnFuncionesID.ShouldBeNull();
		viewModel.Division.ShouldBe(string.Empty);
	}
}
