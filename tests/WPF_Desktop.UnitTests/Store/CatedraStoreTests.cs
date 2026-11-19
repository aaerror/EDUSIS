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

		store.Catedra.ShouldBeNull();
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

	/// <summary>
	/// <c>Guid?</c> y no <c>Guid</c>: con <c>Guid</c> el store nacía en <c>Guid.Empty</c>, un
	/// centinela que el compilador no distingue de una cátedra real. Fija también que guarda un valor
	/// y no un ViewModel.
	/// </summary>
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_propiedad_guarda_un_Guid_nulable_y_no_un_ViewModel()
	{
		typeof(CatedraStore).GetProperty(nameof(CatedraStore.Catedra))!.PropertyType.ShouldBe(typeof(Guid?));
	}
}
