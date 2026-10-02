using System.Globalization;
using Domain.Catedras.SituacionesRevista;
using Shouldly;
using WPF_Desktop.Shared.Converters;
using Xunit;

namespace WPF_Desktop.UnitTests.Converters;

public class CargoConverterTests
{
	private readonly CargoConverter _converter = new();

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(Cargo.Titular, "Titular")]
	[InlineData(Cargo.Suplente, "Suplente")]
	[InlineData(Cargo.Interino, "Interino")]
	public void ConvertBack_devuelve_el_nombre_de_cada_valor_del_enum(Cargo cargo, string esperado)
	{
		var resultado = _converter.ConvertBack(cargo, typeof(string), null!, CultureInfo.InvariantCulture);

		resultado.ShouldBe(esperado);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void ConvertBack_cubre_todos_los_valores_definidos_del_enum()
	{
		foreach (var cargo in Enum.GetValues<Cargo>())
		{
			var resultado = _converter.ConvertBack(cargo, typeof(string), null!, CultureInfo.InvariantCulture);

			resultado.ShouldBe(cargo.ToString());
		}
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void ConvertBack_con_un_valor_fuera_de_rango_devuelve_cadena_vacia_y_no_lanza()
	{
		object? resultado = null;

		Should.NotThrow(() => resultado = _converter.ConvertBack((Cargo)99, typeof(string), null!, CultureInfo.InvariantCulture));

		resultado.ShouldBe(string.Empty);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void ConvertBack_con_null_o_un_valor_que_no_es_enum_devuelve_cadena_vacia()
	{
		_converter.ConvertBack(null!, typeof(string), null!, CultureInfo.InvariantCulture).ShouldBe(string.Empty);
		_converter.ConvertBack("Titular", typeof(string), null!, CultureInfo.InvariantCulture).ShouldBe(string.Empty);
		_converter.ConvertBack(1, typeof(string), null!, CultureInfo.InvariantCulture).ShouldBe(string.Empty);
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData("Titular", Cargo.Titular)]
	[InlineData("Suplente", Cargo.Suplente)]
	[InlineData("Interino", Cargo.Interino)]
	public void Convert_devuelve_el_valor_del_enum_para_cada_nombre(string texto, Cargo esperado)
	{
		var resultado = _converter.Convert(texto, typeof(Cargo), null!, CultureInfo.InvariantCulture);

		resultado.ShouldBe(esperado);
	}

	[Theory]
	[Trait("Categoria", "Unidad")]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("Inexistente")]
	public void Convert_con_texto_vacio_o_desconocido_devuelve_cadena_vacia_y_no_lanza(string? texto)
	{
		object? resultado = null;

		Should.NotThrow(() => resultado = _converter.Convert(texto!, typeof(Cargo), null!, CultureInfo.InvariantCulture));

		resultado.ShouldBe(string.Empty);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Convert_con_un_valor_que_no_es_texto_devuelve_cadena_vacia()
	{
		_converter.Convert(Cargo.Titular, typeof(Cargo), null!, CultureInfo.InvariantCulture).ShouldBe(string.Empty);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Convert_con_un_numero_fuera_de_rango_no_lanza()
	{
		// Enum.TryParse acepta cualquier numérico: "99" produce (Cargo)99 en lugar de rechazarse.
		object? resultado = null;

		Should.NotThrow(() => resultado = _converter.Convert("99", typeof(Cargo), null!, CultureInfo.InvariantCulture));

		resultado.ShouldBe((Cargo)99);
	}
}
