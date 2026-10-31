using Shouldly;
using WPF_Desktop.Store;
using Xunit;

namespace WPF_Desktop.UnitTests.Store;

public class LegajoStoreTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_store_nuevo_no_tiene_persona_elegida()
	{
		var store = new LegajoStore();

		store.PersonaID.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_store_nuevo_no_nace_en_Guid_Empty()
	{
		// UI-34 de docs/todos.md: PersonaID era un Guid no nulable que arrancaba en Guid.Empty, un
		// centinela que significa lo mismo que null pero que el compilador no distingue. Con eso,
		// "nadie eligió una persona" llegaba a Core como un identificador cualquiera.
		var store = new LegajoStore();

		store.PersonaID.ShouldNotBe(Guid.Empty);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_la_persona_guarda_el_Guid_recibido()
	{
		var store = new LegajoStore();
		var personaID = Guid.NewGuid();

		store.PersonaID = personaID;

		store.PersonaID.ShouldBe(personaID);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_el_documento_guarda_el_valor_recibido()
	{
		var store = new LegajoStore();

		store.Documento = "12345678";

		store.Documento.ShouldBe("12345678");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_propiedad_es_un_Guid_nulable_y_no_un_Guid_con_centinela()
	{
		typeof(LegajoStore).GetProperty(nameof(LegajoStore.PersonaID))!.PropertyType.ShouldBe(typeof(Guid?));
	}
}
