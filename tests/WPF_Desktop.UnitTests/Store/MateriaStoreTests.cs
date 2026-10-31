using Shouldly;
using WPF_Desktop.Store;
using Xunit;

namespace WPF_Desktop.UnitTests.Store;

public class MateriaStoreTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_store_nuevo_no_tiene_materia_seleccionada()
	{
		var store = new MateriaStore();

		store.Materia.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_la_materia_guarda_los_dos_identificadores_y_la_descripcion()
	{
		var store = new MateriaStore();
		var cursoID = Guid.NewGuid();
		var materiaID = Guid.NewGuid();

		store.Materia = new MateriaSeleccionada(cursoID, materiaID, "Matematica");

		store.Materia.CursoID.ShouldBe(cursoID);
		store.Materia.MateriaID.ShouldBe(materiaID);
		store.Materia.Descripcion.ShouldBe("Matematica");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_propiedad_no_guarda_un_tipo_de_la_capa_de_presentacion()
	{
		// Violación 14 de docs/todos.md, y era el peor caso de los tres: MateriaViewModel deriva de
		// ObservableValidator y lleva [Required] sobre Descripcion, así que el contexto de
		// navegación arrastraba validación de formulario y podía viajar en estado inválido.
		var tipo = typeof(MateriaStore).GetProperty(nameof(MateriaStore.Materia))!.PropertyType;

		tipo.ShouldBe(typeof(MateriaSeleccionada));
		tipo.Namespace.ShouldNotStartWith("WPF_Desktop.ViewModels");
	}
}
