using Shouldly;
using Xunit;

namespace WPF_Desktop.UnitTests;

public class HumoTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_proyecto_de_pruebas_ejecuta()
	{
		true.ShouldBeTrue();
	}
}
