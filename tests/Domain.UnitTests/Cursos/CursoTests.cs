using Domain.Cursos;
using Domain.Cursos.Exceptions;
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
	public void Un_grado_que_no_esta_en_el_enum_lanza_GradoInexistenteException()
	{
		Should.Throw<GradoInexistenteException>(() => new Curso("Octavo", nameof(NivelEducativo.Secundaria)));
	}

	[Fact]
	public void Un_nivel_educativo_que_no_esta_en_el_enum_lanza_NivelEducativoInexistenteException()
	{
		// El mensaje decía "El grado que se especifico no existe." también acá, por una copia del
		// guard del grado: el usuario leía que fallaba el dato que había cargado bien. Con una
		// excepción por regla, el tipo ya distingue cuál de los dos falló.
		Should.Throw<NivelEducativoInexistenteException>(() => new Curso(nameof(Grado.Primero), "Terciario"));
	}

	/// <remarks>
	/// <c>Enum.TryParse</c> acepta cualquier número y combinaciones con coma; ninguno es un grado.
	/// </remarks>
	[Theory]
	[InlineData("99")]
	[InlineData("Primero, Segundo")]
	[InlineData("")]
	public void Un_grado_numerico_fuera_de_rango_combinado_o_vacio_lanza_GradoInexistenteException(string grado)
	{
		Should.Throw<GradoInexistenteException>(() => Curso.ConvertirGrado(grado));
	}

	[Fact]
	public void La_conversion_del_grado_no_distingue_mayusculas()
	{
		Curso.ConvertirGrado("primero").ShouldBe(Grado.Primero);
	}
	#endregion

	#region Baja
	[Fact]
	public void ValidarSePuedeEliminar_sin_divisiones_no_lanza()
	{
		var curso = new CursoBuilder().Build();

		Should.NotThrow(() => curso.ValidarSePuedeEliminar(0));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(6)]
	public void ValidarSePuedeEliminar_con_divisiones_lanza_CursoConDivisionesException(int divisiones)
	{
		// Una sola división alcanza, y no hay umbral: es lo contrario de la regla que Division tenía
		// —donde Primaria toleraba hasta 14 cursantes— y es deliberado. El curso no negocia: o no le
		// queda ninguna división, o no se elimina.
		var curso = new CursoBuilder().Build();

		var ex = Should.Throw<Exception>(() => curso.ValidarSePuedeEliminar(divisiones));

		ex.GetType().Name.ShouldBe("CursoConDivisionesException");
	}

	[Fact]
	public void ValidarSePuedeEliminar_dice_en_el_mensaje_que_hay_que_eliminar_las_divisiones_primero()
	{
		// El mensaje viaja tal cual a la pantalla, así que tiene que decir qué hacer y no sólo qué
		// pasó: es la diferencia entre "no se puede" y "eliminá primero sus divisiones".
		var curso = new CursoBuilder().Build();

		var ex = Should.Throw<Exception>(() => curso.ValidarSePuedeEliminar(1));

		ex.Message.ShouldContain("divisiones");
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
