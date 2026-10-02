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
	public void Asignar_el_ciclo_lectivo_dispara_el_evento_una_vez()
	{
		var store = new CicloLectivoStore();
		var disparos = 0;
		store.CicloLectivoStoreChanged += () => disparos++;

		store.CicloLectivo = "2031";

		disparos.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_evento_se_dispara_con_el_valor_ya_actualizado()
	{
		var store = new CicloLectivoStore();
		string? observado = null;
		store.CicloLectivoStoreChanged += () => observado = store.CicloLectivo;

		store.CicloLectivo = "2032";

		observado.ShouldBe("2032");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_sin_suscriptores_no_lanza()
	{
		var store = new CicloLectivoStore();

		Should.NotThrow(() => store.CicloLectivo = "2033");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_propiedad_guarda_el_valor_y_no_un_ViewModel()
	{
		typeof(CicloLectivoStore).GetProperty(nameof(CicloLectivoStore.CicloLectivo))!.PropertyType.ShouldBe(typeof(string));
	}
}
