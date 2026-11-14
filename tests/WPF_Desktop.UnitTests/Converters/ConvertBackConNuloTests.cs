using System.Globalization;
using System.Windows.Data;
using Shouldly;
using WPF_Desktop.Shared.Converters;
using Xunit;

namespace WPF_Desktop.UnitTests.Converters;

/// <summary>
/// Un binding cuyo valor todavía no se eligió manda <c>null</c> a <c>ConvertBack</c>. Los tres
/// converters hacían <c>value.ToString()</c> sin guarda y lanzaban <c>NullReferenceException</c> (UI-36).
/// </summary>
public class ConvertBackConNuloTests
{
	public static IEnumerable<object[]> Converters() => new[]
	{
		new object[] { new NivelEducativoConverter() },
		new object[] { new SexoConverter() },
		new object[] { new ViviendaConverter() },
	};

	[Theory]
	[Trait("Categoria", "Unidad")]
	[MemberData(nameof(Converters))]
	public void ConvertBack_con_null_no_escribe_nada_en_el_origen_y_no_lanza(IValueConverter converter)
	{
		object? resultado = null;

		Should.NotThrow(() => resultado = converter.ConvertBack(null!, typeof(object), null!, CultureInfo.InvariantCulture));

		resultado.ShouldBe(Binding.DoNothing);
	}
}
