using Domain.Cursos;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Cursos;

/// <summary>
/// Entidad <see cref="Division"/>: validación del nombre, cargo de preceptor y colección de
/// cursantes. Las excepciones del módulo son <c>internal</c>: se verifican por el nombre del
/// tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class DivisionTests
{
	#region Nombre
	[Fact]
	public void El_constructor_normaliza_la_descripcion_a_mayuscula()
	{
		var division = new Division(" a ");

		division.Descripcion.ShouldBe("A");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void El_constructor_sin_descripcion_lanza_ArgumentNullException(string descripcion)
	{
		Should.Throw<ArgumentNullException>(() => new Division(descripcion));
	}

	[Theory]
	[InlineData("AB")]
	[InlineData("1")]
	[InlineData("%")]
	public void El_constructor_con_una_descripcion_que_no_es_una_unica_letra_lanza_ArgumentException(string descripcion)
	{
		Should.Throw<ArgumentException>(() => new Division(descripcion));
	}
	#endregion

	#region Preceptor
	[Fact]
	public void AsignarPreceptor_ocupa_el_cargo_vacante()
	{
		var division = new Division("A");
		var preceptor = Guid.NewGuid();

		division.AsignarPreceptor(preceptor);

		division.EstaCargoPreceptorVacante().ShouldBeFalse();
		division.Preceptor.ShouldBe(preceptor);
	}

	[Fact]
	public void AsignarPreceptor_sobre_un_cargo_ocupado_lanza_CargoPreceptorNoDisponibleException()
	{
		var division = new Division("A");
		division.AsignarPreceptor(Guid.NewGuid());

		var ex = Should.Throw<Exception>(() => division.AsignarPreceptor(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("CargoPreceptorNoDisponibleException");
	}

	[Fact]
	public void AsignarPreceptor_con_Guid_vacio_lanza_SinDatosPreceptorException()
	{
		var division = new Division("A");

		var ex = Should.Throw<Exception>(() => division.AsignarPreceptor(Guid.Empty));

		ex.GetType().Name.ShouldBe("SinDatosPreceptorException");
	}

	[Fact]
	public void QuitarPreceptor_libera_el_cargo_ocupado()
	{
		var division = new Division("A");
		division.AsignarPreceptor(Guid.NewGuid());

		division.QuitarPreceptor();

		division.EstaCargoPreceptorVacante().ShouldBeTrue();
	}

	[Fact]
	public void QuitarPreceptor_sobre_un_cargo_vacante_lanza_CargoPreceptorDisponibleException()
	{
		var division = new Division("A");

		var ex = Should.Throw<Exception>(() => division.QuitarPreceptor());

		ex.GetType().Name.ShouldBe("CargoPreceptorDisponibleException");
	}
	#endregion

	#region Cursantes
	[Fact]
	public void AgregarCursante_incorpora_al_alumno_a_la_division()
	{
		var division = new Division("A");
		var cursante = Guid.NewGuid();

		division.AgregarCursante(cursante);

		division.TotalAlumnos.ShouldBe(1);
		division.ExisteCursante(cursante).ShouldBeTrue();
	}

	[Fact]
	public void AgregarCursante_dos_veces_al_mismo_alumno_lanza_CursanteRegistradoException()
	{
		var division = new Division("A");
		var cursante = Guid.NewGuid();
		division.AgregarCursante(cursante);

		var ex = Should.Throw<Exception>(() => division.AgregarCursante(cursante));

		ex.GetType().Name.ShouldBe("CursanteRegistradoException");
	}

	[Fact]
	public void QuitarCursante_remueve_al_alumno()
	{
		var division = new Division("A");
		var cursante = Guid.NewGuid();
		division.AgregarCursante(cursante);

		division.QuitarCursante(cursante);

		division.TotalAlumnos.ShouldBe(0);
	}

	[Fact]
	public void QuitarCursante_de_un_alumno_no_registrado_lanza_CursanteNoEncontradoException()
	{
		var division = new Division("A");

		var ex = Should.Throw<Exception>(() => division.QuitarCursante(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("CursanteNoEncontradoException");
	}

	[Fact]
	public void ExisteCursante_con_Guid_vacio_lanza_NullReferenceException()
	{
		var division = new Division("A");

		Should.Throw<NullReferenceException>(() => division.ExisteCursante(Guid.Empty));
	}
	#endregion
}
