using Shouldly;
using WPF_Desktop.Store;
using Xunit;

namespace WPF_Desktop.UnitTests.Store;

public class CursoStoreTests
{
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Un_store_nuevo_no_tiene_curso_seleccionado()
	{
		var store = new CursoStore();

		store.Curso.ShouldBeNull();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Asignar_el_curso_guarda_el_identificador_y_las_dos_etiquetas()
	{
		var store = new CursoStore();
		var cursoID = Guid.NewGuid();

		store.Curso = new CursoSeleccionado(cursoID, "Primero", "Secundaria");

		store.Curso.CursoID.ShouldBe(cursoID);
		store.Curso.Grado.ShouldBe("Primero");
		store.Curso.NivelEducativo.ShouldBe("Secundaria");
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void La_propiedad_no_guarda_un_tipo_de_la_capa_de_presentacion()
	{
		// Violación 14 de docs/todos.md: el store guardaba un CursoViewModel, lo que ataba las
		// pantallas destino al tipo de presentación de la pantalla de cursos y arrastraba comandos
		// y validación hasta un Singleton.
		var tipo = typeof(CursoStore).GetProperty(nameof(CursoStore.Curso))!.PropertyType;

		tipo.ShouldBe(typeof(CursoSeleccionado));
		tipo.Namespace.ShouldNotStartWith("WPF_Desktop.ViewModels");
	}
}
