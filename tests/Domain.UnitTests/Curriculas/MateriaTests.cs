using Domain.Materias;
using Domain.Materias.DomainEvents;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Curriculas;

/// <summary>
/// Entidad <see cref="Materia"/>: alta (con el evento <see cref="MateriaRegistradaEvent"/>
/// encolado), edición y baja (<see cref="MateriaEliminadaEvent"/>). Los cargos docentes y los
/// horarios pasaron al agregado propio <c>Domain.Catedras.Catedra</c> (ver
/// <c>CatedraTests</c> y <c>SituacionRevistaTests</c>); el alta de calificaciones sigue
/// comentada en <see cref="Domain.Cursantes.Cursante"/> a propósito (FR-013) y no se prueba.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class MateriaTests
{
	#region Alta
	[Fact]
	public void Una_materia_nueva_guarda_descripcion_y_horas_y_encola_MateriaRegistradaEvent()
	{
		var curriculaID = Guid.NewGuid();

		var materia = new Materia(curriculaID, "Matemática", 4);

		materia.CurriculaID.ShouldBe(curriculaID);
		materia.Descripcion.ShouldBe("Matemática");
		materia.HorasCatedra.ShouldBe(4);
		var evento = materia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<MateriaRegistradaEvent>();
		evento.MateriaID.ShouldBe(materia.Id);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Una_materia_sin_descripcion_lanza_ArgumentNullException(string descripcion)
	{
		Should.Throw<ArgumentNullException>(() => new Materia(Guid.NewGuid(), descripcion, 4));
	}

	[Fact]
	public void Una_materia_sin_curricula_lanza_ArgumentNullException()
	{
		Should.Throw<ArgumentNullException>(() => new Materia(Guid.Empty, "Matemática", 4));
	}

	[Fact]
	public void Una_materia_con_menos_de_una_hora_catedra_lanza_ArgumentException()
	{
		Should.Throw<ArgumentException>(() => new Materia(Guid.NewGuid(), "Matemática", 0));
	}

	[Fact]
	public void ModificarMateria_actualiza_descripcion_y_carga_horaria()
	{
		var materia = new MateriaBuilder().Build();

		materia.ModificarMateria("Análisis Matemático", 6);

		materia.Descripcion.ShouldBe("Análisis Matemático");
		materia.HorasCatedra.ShouldBe(6);
	}

	[Fact]
	public void ModificarMateria_con_carga_horaria_no_positiva_lanza_ArgumentException()
	{
		var materia = new MateriaBuilder().Build();

		Should.Throw<ArgumentException>(() => materia.ModificarMateria("Matemática", 0));
	}
	#endregion

	#region Baja
	[Fact]
	public void Eliminar_encola_MateriaEliminadaEvent()
	{
		var materia = new MateriaBuilder().Build();
		materia.LiberarEventos();

		materia.Eliminar();

		var evento = materia.Eventos.ShouldHaveSingleItem().ShouldBeOfType<MateriaEliminadaEvent>();
		evento.MateriaID.ShouldBe(materia.Id);
	}
	#endregion
}
