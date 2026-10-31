using Shouldly;
using WPF_Desktop.Store;
using Xunit;

namespace WPF_Desktop.UnitTests.Store;

public class CatedraStoreTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_store_nuevo_no_tiene_catedra_seleccionada()
	{
		var store = new CatedraStore();

		store.Catedra.ShouldBe(Guid.Empty);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_la_catedra_guarda_el_Guid_recibido()
	{
		var store = new CatedraStore();
		var catedraID = Guid.NewGuid();

		store.Catedra = catedraID;

		store.Catedra.ShouldBe(catedraID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_propiedad_guarda_un_Guid_y_no_un_ViewModel()
	{
		typeof(CatedraStore).GetProperty(nameof(CatedraStore.Catedra))!.PropertyType.ShouldBe(typeof(Guid));
	}
}
