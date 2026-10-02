using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioCursantes.DTOs.Responses;
using Core.ServicioCursos;
using NSubstitute;
using Shouldly;
using System.Collections.ObjectModel;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

public class CalificacionViewModelTests
{
	private static CalificacionResponse CrearResponse(bool rindio = true, bool aprobado = false, double? nota = 4.5, string? observacion = "Debe recuperar")
	{
		return new CalificacionResponse(
			CalificacionID: Guid.NewGuid(),
			MateriaID: Guid.NewGuid(),
			Materia: "Matematica",
			Fecha: new DateTime(2025, 6, 12),
			Instancia: "Recuperatorio",
			Rindio: rindio,
			Nota: nota,
			Aprobado: aprobado,
			Observacion: observacion);
	}

	private static CalificacionViewModel Crear(CalificacionResponse? response)
	{
		return new CalificacionViewModel(Substitute.For<IServicioCurso>(), response);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_constructor_mapea_cada_campo_del_response()
	{
		var response = CrearResponse();

		var viewModel = Crear(response);

		viewModel.CalificacionID.ShouldBe(response.CalificacionID);
		viewModel.MateriaID.ShouldBe(response.MateriaID);
		viewModel.CalificacionID.ShouldNotBe(viewModel.MateriaID);
		viewModel.Materia.ShouldBe("Matematica");
		viewModel.Fecha.ShouldBe(new DateTime(2025, 6, 12));
		viewModel.Instancia.ShouldBe("Recuperatorio");
		viewModel.Rindio.ShouldBeTrue();
		viewModel.Nota.ShouldBe(4.5);
		viewModel.Aprobado.ShouldBeFalse();
		viewModel.Observacion.ShouldBe("Debe recuperar");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Rindio_y_Aprobado_no_se_cruzan()
	{
		var rindioSinAprobar = Crear(CrearResponse(rindio: true, aprobado: false));
		var noRindioAprobado = Crear(CrearResponse(rindio: false, aprobado: true));

		rindioSinAprobar.Rindio.ShouldBeTrue();
		rindioSinAprobar.Aprobado.ShouldBeFalse();
		noRindioAprobado.Rindio.ShouldBeFalse();
		noRindioAprobado.Aprobado.ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Una_calificacion_sin_nota_ni_observacion_deja_ambas_en_nulo()
	{
		var viewModel = Crear(CrearResponse(rindio: false, nota: null, observacion: null));

		viewModel.Nota.ShouldBeNull();
		viewModel.Observacion.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Instancia_es_un_texto_y_Fecha_no_admite_nulos()
	{
		var tipo = typeof(CalificacionViewModel);

		tipo.GetProperty(nameof(CalificacionViewModel.Instancia))!.PropertyType.ShouldBe(typeof(string));
		tipo.GetProperty(nameof(CalificacionViewModel.Fecha))!.PropertyType.ShouldBe(typeof(DateTime));
		tipo.GetProperty(nameof(CalificacionViewModel.Nota))!.PropertyType.ShouldBe(typeof(double?));
		tipo.GetProperty(nameof(CalificacionViewModel.Aprobado))!.PropertyType.ShouldBe(typeof(bool));
		tipo.GetProperty(nameof(CalificacionViewModel.Rindio))!.PropertyType.ShouldBe(typeof(bool));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_propiedad_Asistencia_ya_no_existe()
	{
		typeof(CalificacionViewModel).GetProperty("Asistencia").ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Es_un_ObservableValidator()
	{
		Crear(CrearResponse()).ShouldBeAssignableTo<ObservableValidator>();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_coleccion_de_materias_es_de_ViewModels_y_no_de_DTOs()
	{
		typeof(CalificacionViewModel).GetProperty(nameof(CalificacionViewModel.Materias))!.PropertyType
			.ShouldBe(typeof(ObservableCollection<MateriaViewModel>));

		Crear(CrearResponse()).Materias.ShouldNotBeNull();
		Crear(CrearResponse()).Materias.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void MostrarNota_acompana_a_Rindio_desde_el_constructor()
	{
		Crear(CrearResponse(rindio: true)).MostrarNota.ShouldBeTrue();
		Crear(CrearResponse(rindio: false)).MostrarNota.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Cambiar_Rindio_actualiza_MostrarNota()
	{
		var viewModel = Crear(CrearResponse(rindio: false));

		viewModel.Rindio = true;
		viewModel.MostrarNota.ShouldBeTrue();

		viewModel.Rindio = false;
		viewModel.MostrarNota.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Sin_response_el_ViewModel_queda_listo_para_una_calificacion_nueva()
	{
		var viewModel = Crear(null);

		viewModel.CalificacionID.ShouldBe(Guid.Empty);
		viewModel.MateriaID.ShouldBe(Guid.Empty);
		viewModel.Materia.ShouldBe(string.Empty);
		viewModel.Instancia.ShouldBe(string.Empty);
		viewModel.Fecha.ShouldBe(DateTime.Today);
		viewModel.Rindio.ShouldBeFalse();
		viewModel.MostrarNota.ShouldBeFalse();
		viewModel.Nota.ShouldBeNull();
		viewModel.Observacion.ShouldBeNull();
		viewModel.Materias.ShouldBeEmpty();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Todo_campo_del_response_tiene_una_propiedad_homonima_del_mismo_tipo()
	{
		foreach (var campo in typeof(CalificacionResponse).GetProperties().Where(p => p.Name != "EqualityContract"))
		{
			var propiedad = typeof(CalificacionViewModel).GetProperty(campo.Name);

			propiedad.ShouldNotBeNull($"CalificacionViewModel no expone '{campo.Name}'");
			propiedad!.PropertyType.ShouldBe(campo.PropertyType, $"tipo de '{campo.Name}'");
		}
	}
}
