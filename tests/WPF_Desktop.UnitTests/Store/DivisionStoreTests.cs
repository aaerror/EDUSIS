using Shouldly;
using WPF_Desktop.Store;
using Xunit;

namespace WPF_Desktop.UnitTests.Store;

public class DivisionStoreTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_store_nuevo_no_tiene_division_seleccionada()
	{
		var store = new DivisionStore();

		store.Division.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_la_division_guarda_el_identificador_recibido()
	{
		var store = new DivisionStore();
		var divisionID = Guid.NewGuid();

		store.Division = new DivisionSeleccionada(divisionID);

		store.Division.DivisionID.ShouldBe(divisionID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_propiedad_no_guarda_un_tipo_de_la_capa_de_presentacion()
	{
		// Violación 14 de docs/todos.md: el store guardaba un DivisionViewModel. El destino sólo
		// necesita el identificador, y el DivisionResponse completo habría dejado PreceptorID,
		// Preceptor y Cursantes obsoletos dentro de un Singleton que nadie limpia.
		var tipo = typeof(DivisionStore).GetProperty(nameof(DivisionStore.Division))!.PropertyType;

		tipo.ShouldBe(typeof(DivisionSeleccionada));
		tipo.Namespace.ShouldNotStartWith("WPF_Desktop.ViewModels");
	}
}
