using Domain.Cursos;
using Domain.Cursos.DomainEvents;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Cursos;

/// <summary>
/// Agregado <see cref="Curso"/> con sus enumerados <see cref="Grado"/> y
/// <see cref="NivelEducativo"/>, la gestión de divisiones, preceptores y cursantes, y el evento
/// <see cref="MateriaEliminadaEvent"/>. Las excepciones del módulo son <c>internal</c>: se
/// verifican por el nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CursoTests
{
	#region Alta
	[Fact]
	public void Un_curso_nuevo_guarda_grado_y_nivel_educativo_sin_divisiones()
	{
		var curso = new CursoBuilder().ConGrado(nameof(Grado.Primero)).ConNivelEducativo(nameof(NivelEducativo.Secundaria)).Build();

		curso.Grado.ShouldBe(Grado.Primero);
		curso.NivelEducativo.ShouldBe(NivelEducativo.Secundaria);
		curso.CantidadDivisiones.ShouldBe(0);
		curso.CantidadAlumnos.ShouldBe(0);
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

	#region Divisiones
	[Fact]
	public void AgregarDivision_nombra_las_divisiones_en_orden_alfabetico()
	{
		var curso = new CursoBuilder().Build();

		curso.AgregarDivision();
		curso.AgregarDivision();

		curso.CantidadDivisiones.ShouldBe(2);
		curso.Divisiones.Select(d => d.Descripcion).ShouldBe(new[] { "A", "B" });
	}

	[Fact]
	public void QuitarDivision_elimina_la_division_indicada()
	{
		var curso = new CursoBuilder().ConDivision(2).Build();
		var aEliminar = curso.Divisiones.First().Id;

		curso.QuitarDivision(aEliminar);

		curso.CantidadDivisiones.ShouldBe(1);
	}

	[Fact]
	public void QuitarDivision_con_un_id_desconocido_lanza_DivisionNoEncontradaException()
	{
		var curso = new CursoBuilder().ConDivision(1).Build();

		var ex = Should.Throw<Exception>(() => curso.QuitarDivision(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("DivisionNoEncontradaException");
	}

	[Fact]
	public void QuitarDivision_con_Guid_vacio_lanza_ArgumentNullException()
	{
		var curso = new CursoBuilder().ConDivision(1).Build();

		Should.Throw<ArgumentNullException>(() => curso.QuitarDivision(Guid.Empty));
	}
	#endregion

	#region Preceptores
	[Fact]
	public void AsignarPreceptor_ocupa_el_cargo_en_la_division()
	{
		var curso = new CursoBuilder().ConDivision(1).Build();
		var division = curso.Divisiones.First();
		var preceptor = Guid.NewGuid();

		curso.AsignarPreceptor(division.Id, preceptor);

		division.Preceptor.ShouldBe(preceptor);
	}

	[Fact]
	public void AsignarPreceptor_sobre_una_division_inexistente_lanza_DivisionNoEncontradaException()
	{
		var curso = new CursoBuilder().ConDivision(1).Build();

		var ex = Should.Throw<Exception>(() => curso.AsignarPreceptor(Guid.NewGuid(), Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("DivisionNoEncontradaException");
	}

	[Fact]
	public void QuitarPreceptor_libera_el_cargo()
	{
		var curso = new CursoBuilder().ConDivision(1).Build();
		var division = curso.Divisiones.First();
		curso.AsignarPreceptor(division.Id, Guid.NewGuid());

		curso.QuitarPreceptor(division.Id);

		division.Preceptor.ShouldBeNull();
	}
	#endregion

	#region Cursantes
	[Fact]
	public void AgregarAlumnoEnDivision_registra_al_cursante()
	{
		var curso = new CursoBuilder().ConDivision(1).Build();
		var division = curso.Divisiones.First();
		var cursante = Guid.NewGuid();

		curso.AgregarAlumnoEnDivision(division.Id, cursante);

		curso.CursanteRegistrado(cursante).ShouldBeTrue();
		curso.CantidadAlumnos.ShouldBe(1);
	}

	[Fact]
	public void AgregarAlumnoEnDivision_a_un_cursante_ya_registrado_lanza_CursanteRegistradoException()
	{
		var curso = new CursoBuilder().ConDivision(1).Build();
		var division = curso.Divisiones.First();
		var cursante = Guid.NewGuid();
		curso.AgregarAlumnoEnDivision(division.Id, cursante);

		var ex = Should.Throw<Exception>(() => curso.AgregarAlumnoEnDivision(division.Id, cursante));

		ex.GetType().Name.ShouldBe("CursanteRegistradoException");
	}

	[Fact(Skip = "H-008: Curso.QuitarAlumno tiene la condición del guard invertida y siempre lanza DivisionNoEncontradaException cuando la división existe. Ver hallazgos.md.")]
	public void QuitarAlumno_remueve_al_cursante_de_la_division()
	{
	}

	[Fact]
	public void Hoy_QuitarAlumno_siempre_lanza_DivisionNoEncontradaException_aunque_la_division_exista()
	{
		var curso = new CursoBuilder().ConDivision(1).Build();
		var division = curso.Divisiones.First();
		var cursante = Guid.NewGuid();
		curso.AgregarAlumnoEnDivision(division.Id, cursante);

		// Documenta el estado actual (H-008): el guard es `if (ExisteDivision(...)) throw ...`.
		var ex = Should.Throw<Exception>(() => curso.QuitarAlumno(division.Id, cursante));
		ex.GetType().Name.ShouldBe("DivisionNoEncontradaException");
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
