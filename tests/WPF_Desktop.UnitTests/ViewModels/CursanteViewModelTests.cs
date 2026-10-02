using Core.ServicioCursantes.DTOs.Responses;
using Shouldly;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

public class CursanteViewModelTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_constructor_mapea_cada_campo_del_response()
	{
		var response = new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Gomez, Luis", "40123456", 17, true);

		var viewModel = new CursanteViewModel(response);

		viewModel.CursanteID.ShouldBe(response.CursanteID);
		viewModel.AlumnoID.ShouldBe(response.AlumnoID);
		viewModel.NombreCompleto.ShouldBe("Gomez, Luis");
		viewModel.Documento.ShouldBe("40123456");
		viewModel.Edad.ShouldBe(17);
		viewModel.EsRecursante.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void CursanteID_queda_asignado_y_distinto_de_AlumnoID()
	{
		// Es la clave de todos los requests de calificacion y de asistencia: no puede quedar en Guid.Empty.
		var response = new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Gomez, Luis", "40123456", 17, false);

		var viewModel = new CursanteViewModel(response);

		viewModel.CursanteID.ShouldNotBe(Guid.Empty);
		viewModel.CursanteID.ShouldNotBe(viewModel.AlumnoID);
		viewModel.AlumnoID.ShouldNotBe(Guid.Empty);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_cursante_que_no_recursa_deja_EsRecursante_en_falso()
	{
		var response = new CursanteResponse(Guid.NewGuid(), Guid.NewGuid(), "Gomez, Luis", "40123456", 17, false);

		var viewModel = new CursanteViewModel(response);

		viewModel.EsRecursante.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Edad_es_un_entero_y_no_un_texto()
	{
		typeof(CursanteViewModel).GetProperty(nameof(CursanteViewModel.Edad))!.PropertyType.ShouldBe(typeof(int));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_response_nulo_no_lanza_y_deja_los_valores_por_defecto()
	{
		CursanteViewModel? viewModel = null;

		Should.NotThrow(() => { viewModel = new CursanteViewModel(null!); });

		viewModel!.CursanteID.ShouldBe(Guid.Empty);
		viewModel.AlumnoID.ShouldBe(Guid.Empty);
		viewModel.NombreCompleto.ShouldBe(string.Empty);
		viewModel.Documento.ShouldBe(string.Empty);
		viewModel.Edad.ShouldBe(0);
		viewModel.EsRecursante.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todo_campo_del_response_tiene_una_propiedad_homonima_del_mismo_tipo()
	{
		foreach (var campo in typeof(CursanteResponse).GetProperties().Where(p => p.Name != "EqualityContract"))
		{
			var propiedad = typeof(CursanteViewModel).GetProperty(campo.Name);

			propiedad.ShouldNotBeNull($"CursanteViewModel no expone '{campo.Name}'");
			propiedad!.PropertyType.ShouldBe(campo.PropertyType, $"tipo de '{campo.Name}'");
		}
	}
}
