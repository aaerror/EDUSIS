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
	public void Asignar_la_catedra_dispara_el_evento_una_vez()
	{
		var store = new CatedraStore();
		var disparos = 0;
		store.CatedraStoreChanged += () => disparos++;

		store.Catedra = Guid.NewGuid();

		disparos.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_evento_se_dispara_con_el_valor_ya_actualizado()
	{
		var store = new CatedraStore();
		var catedraID = Guid.NewGuid();
		var observado = Guid.Empty;
		store.CatedraStoreChanged += () => observado = store.Catedra;

		store.Catedra = catedraID;

		observado.ShouldBe(catedraID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_sin_suscriptores_no_lanza()
	{
		var store = new CatedraStore();

		Should.NotThrow(() => store.Catedra = Guid.NewGuid());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_propiedad_guarda_un_Guid_y_no_un_ViewModel()
	{
		typeof(CatedraStore).GetProperty(nameof(CatedraStore.Catedra))!.PropertyType.ShouldBe(typeof(Guid));
	}
}
