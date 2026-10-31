using Shouldly;
using WPF_Desktop.Store;
using Xunit;

namespace WPF_Desktop.UnitTests.Store;

public class CicloLectivoStoreTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_store_nuevo_inicializa_en_el_anio_en_curso()
	{
		var antes = DateTime.Today.Year.ToString();
		var store = new CicloLectivoStore();
		var despues = DateTime.Today.Year.ToString();

		// Se acepta cualquiera de los dos para no fallar si la prueba cruza el cambio de año.
		new[] { antes, despues }.ShouldContain(store.CicloLectivo);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_el_ciclo_lectivo_guarda_el_valor_recibido()
	{
		var store = new CicloLectivoStore();

		store.CicloLectivo = "2031";

		store.CicloLectivo.ShouldBe("2031");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_propiedad_guarda_el_valor_y_no_un_ViewModel()
	{
		typeof(CicloLectivoStore).GetProperty(nameof(CicloLectivoStore.CicloLectivo))!.PropertyType.ShouldBe(typeof(string));
	}
}
