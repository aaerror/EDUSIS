using System.Globalization;
using Domain.Shared;
using Shouldly;
using WPF_Desktop.Shared.Converters;
using Xunit;

namespace WPF_Desktop.UnitTests.Converters;

public class NivelEducativoConverterTests
{
	private readonly NivelEducativoConverter _converter = new();

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("Primaria", NivelEducativo.Primaria)]
	[InlineData("Secundaria", NivelEducativo.Secundaria)]
	public void Convert_devuelve_el_valor_del_enum_para_cada_nombre(string texto, NivelEducativo esperado)
	{
		var resultado = _converter.Convert(texto, typeof(NivelEducativo), null!, CultureInfo.InvariantCulture);

		resultado.ShouldBe(esperado);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Convert_cubre_todos_los_valores_definidos_del_enum()
	{
		foreach (var nivel in Enum.GetValues<NivelEducativo>())
		{
			var resultado = _converter.Convert(nivel.ToString(), typeof(NivelEducativo), null!, CultureInfo.InvariantCulture);

			resultado.ShouldBe(nivel);
		}
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("Terciaria")]
	public void Convert_con_texto_vacio_o_desconocido_devuelve_cadena_vacia_y_no_lanza(string? texto)
	{
		object? resultado = null;

		Should.NotThrow(() => resultado = _converter.Convert(texto!, typeof(NivelEducativo), null!, CultureInfo.InvariantCulture));

		resultado.ShouldBe(string.Empty);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Convert_con_un_numero_fuera_de_rango_no_lanza()
	{
		// Enum.TryParse acepta cualquier numérico: "99" produce (NivelEducativo)99 en lugar de rechazarse.
		object? resultado = null;

		Should.NotThrow(() => resultado = _converter.Convert("99", typeof(NivelEducativo), null!, CultureInfo.InvariantCulture));

		resultado.ShouldBe((NivelEducativo)99);
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(NivelEducativo.Primaria)]
	[InlineData(NivelEducativo.Secundaria)]
	public void ConvertBack_devuelve_el_mismo_valor_del_enum(NivelEducativo nivel)
	{
		var resultado = _converter.ConvertBack(nivel, typeof(NivelEducativo), null!, CultureInfo.InvariantCulture);

		resultado.ShouldBe(nivel);
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("Primaria", NivelEducativo.Primaria)]
	[InlineData("Secundaria", NivelEducativo.Secundaria)]
	public void ConvertBack_interpreta_el_texto_de_cada_valor(string texto, NivelEducativo esperado)
	{
		var resultado = _converter.ConvertBack(texto, typeof(NivelEducativo), null!, CultureInfo.InvariantCulture);

		resultado.ShouldBe(esperado);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void ConvertBack_con_un_valor_enum_fuera_de_rango_no_lanza()
	{
		// ((NivelEducativo)99).ToString() == "99", que Enum.Parse acepta como numérico.
		object? resultado = null;

		Should.NotThrow(() => resultado = _converter.ConvertBack((NivelEducativo)99, typeof(NivelEducativo), null!, CultureInfo.InvariantCulture));

		resultado.ShouldBe((NivelEducativo)99);
	}
}
