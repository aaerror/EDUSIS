using Domain.Cursos;
using Domain.Materias.DomainEvents;
using Domain.Shared;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Cursos;

/// <summary>
/// Agregado <see cref="Curso"/> con sus enumerados <see cref="Grado"/> y
/// <see cref="NivelEducativo"/>, y el evento <see cref="MateriaEliminadaEvent"/> (movido a
/// <c>Domain.Materias.DomainEvents</c>: lo emite <c>Materia</c>, no <c>Curso</c>). Las
/// divisiones, los preceptores y los cursantes pasaron al agregado propio
/// <see cref="Domain.Divisiones.Division"/> (ver <c>Domain.UnitTests.Divisiones.DivisionTests</c>):
/// <c>Curso</c> hoy sólo guarda grado y nivel educativo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CursoTests
{
	#region Alta
	[Fact]
	public void Un_curso_nuevo_guarda_grado_y_nivel_educativo()
	{
		var curso = new CursoBuilder().ConGrado(nameof(Grado.Primero)).ConNivelEducativo(nameof(NivelEducativo.Secundaria)).Build();

		curso.Grado.ShouldBe(Grado.Primero);
		curso.NivelEducativo.ShouldBe(NivelEducativo.Secundaria);
	}

	[Fact]
	public void Un_grado_que_no_esta_en_el_enum_lanza_ArgumentException()
	{
		Should.Throw<ArgumentException>(() => new Curso("Octavo", nameof(NivelEducativo.Secundaria)));
	}

	[Fact]
	public void Un_nivel_educativo_que_no_esta_en_el_enum_lanza_ArgumentException()
	{
		Should.Throw<ArgumentException>(() => new Curso(nameof(Grado.Primero), "Terciario"));
	}
	#endregion

	#region MateriaEliminadaEvent
	[Fact]
	public void MateriaEliminadaEvent_conserva_el_id_de_materia_y_compara_por_valor()
	{
		var materiaID = Guid.NewGuid();

		var uno = new MateriaEliminadaEvent(materiaID);
		var otro = new MateriaEliminadaEvent(materiaID);

		uno.MateriaID.ShouldBe(materiaID);
		uno.ShouldBe(otro);
	}
	#endregion
}
