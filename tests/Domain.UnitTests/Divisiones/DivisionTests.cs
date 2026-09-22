using Domain.Divisiones;
using Domain.Shared;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Divisiones;

/// <summary>
/// Agregado <see cref="Division"/>: alta, numeración automática (<see cref="Division.Siguiente"/>),
/// cargo de preceptor y las validaciones de cupo/baja. La gestión de cursantes por división pasó
/// a <see cref="Domain.Cursantes.Cursante"/> (con conteos vía <c>ICursanteRepository</c>), así
/// que no vive más acá. Las excepciones del módulo son <c>internal</c>: se verifican por el
/// nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class DivisionTests
{
	#region Alta
	[Fact]
	public void El_constructor_normaliza_la_descripcion_a_mayuscula()
	{
		var division = new DivisionBuilder().ConDescripcion(" a ").Build();

		division.Descripcion.ShouldBe("A");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void El_constructor_sin_descripcion_lanza_ArgumentNullException(string descripcion)
	{
		Should.Throw<ArgumentNullException>(() => new DivisionBuilder().ConDescripcion(descripcion).Build());
	}

	[Theory]
	[InlineData("AB")]
	[InlineData("1")]
	[InlineData("%")]
	public void El_constructor_con_una_descripcion_que_no_es_una_unica_letra_lanza_ArgumentException(string descripcion)
	{
		Should.Throw<ArgumentException>(() => new DivisionBuilder().ConDescripcion(descripcion).Build());
	}

	[Fact]
	public void El_constructor_sin_curso_lanza_SinDatosCursoException()
	{
		var ex = Should.Throw<Exception>(() => new Division(Guid.Empty, "A"));

		ex.GetType().Name.ShouldBe("SinDatosCursoException");
	}
	#endregion

	#region Siguiente
	[Fact]
	public void Siguiente_sin_divisiones_previas_devuelve_A()
	{
		var division = Division.Siguiente(Guid.NewGuid(), Array.Empty<string>());

		division.Descripcion.ShouldBe("A");
	}

	[Fact]
	public void Siguiente_con_divisiones_previas_devuelve_la_letra_siguiente_a_la_ultima()
	{
		var division = Division.Siguiente(Guid.NewGuid(), new[] { "A", "B" });

		division.Descripcion.ShouldBe("C");
	}

	[Fact]
	public void Siguiente_despues_de_Z_no_tiene_tope_y_hoy_lanza_ArgumentException()
	{
		// TODO conocido (Domain/Divisiones/Division.cs, Siguiente): "sin tope tras 'Z'".
		// char.ConvertFromUtf32('Z' + 1) da '[', que el constructor de Division rechaza por no
		// ser una letra (regex ^[a-zA-Z]{1}$). El síntoma hoy es un ArgumentException genérico
		// ("la división del curso debe ser una letra") en vez de una regla de negocio explícita
		// sobre el máximo de divisiones por curso. Este test documenta ese estado actual.
		Should.Throw<ArgumentException>(() => Division.Siguiente(Guid.NewGuid(), new[] { "Z" }));
	}
	#endregion

	#region Preceptor
	[Fact]
	public void AsignarPreceptor_ocupa_el_cargo_vacante()
	{
		var division = new DivisionBuilder().Build();
		var preceptor = Guid.NewGuid();

		division.AsignarPreceptor(preceptor);

		division.EstaCargoPreceptorVacante().ShouldBeFalse();
		division.Preceptor.ShouldBe(preceptor);
	}

	[Fact]
	public void AsignarPreceptor_sobre_un_cargo_ocupado_lanza_CargoPreceptorNoDisponibleException()
	{
		var division = new DivisionBuilder().Build();
		division.AsignarPreceptor(Guid.NewGuid());

		var ex = Should.Throw<Exception>(() => division.AsignarPreceptor(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("CargoPreceptorNoDisponibleException");
	}

	[Fact]
	public void AsignarPreceptor_con_Guid_vacio_lanza_SinDatosPreceptorException()
	{
		var division = new DivisionBuilder().Build();

		var ex = Should.Throw<Exception>(() => division.AsignarPreceptor(Guid.Empty));

		ex.GetType().Name.ShouldBe("SinDatosPreceptorException");
	}

	[Fact]
	public void QuitarPreceptor_libera_el_cargo_ocupado()
	{
		var division = new DivisionBuilder().Build();
		division.AsignarPreceptor(Guid.NewGuid());

		division.QuitarPreceptor();

		division.EstaCargoPreceptorVacante().ShouldBeTrue();
	}

	[Fact]
	public void QuitarPreceptor_sobre_un_cargo_vacante_lanza_CargoPreceptorDisponibleException()
	{
		var division = new DivisionBuilder().Build();

		var ex = Should.Throw<Exception>(() => division.QuitarPreceptor());

		ex.GetType().Name.ShouldBe("CargoPreceptorDisponibleException");
	}
	#endregion

	#region Cupo
	[Fact]
	public void ValidarCupoDisponible_por_debajo_del_maximo_no_lanza()
	{
		var division = new DivisionBuilder().Build();

		Should.NotThrow(() => division.ValidarCupoDisponible(34));
	}

	[Fact]
	public void ValidarCupoDisponible_en_el_maximo_lanza_CupoMaximoAlcanzadoException()
	{
		var division = new DivisionBuilder().Build();

		var ex = Should.Throw<Exception>(() => division.ValidarCupoDisponible(35));

		ex.GetType().Name.ShouldBe("CupoMaximoAlcanzadoException");
	}

	[Fact]
	public void ValidarCupoDisponible_por_encima_del_maximo_lanza_CupoMaximoAlcanzadoException()
	{
		var division = new DivisionBuilder().Build();

		var ex = Should.Throw<Exception>(() => division.ValidarCupoDisponible(40));

		ex.GetType().Name.ShouldBe("CupoMaximoAlcanzadoException");
	}
	#endregion

	#region Baja
	[Fact]
	public void ValidarSePuedeEliminar_de_secundaria_lanza_DivisionNoEliminableException()
	{
		var division = new DivisionBuilder().Build();

		var ex = Should.Throw<Exception>(() => division.ValidarSePuedeEliminar(NivelEducativo.Secundaria, 0));

		ex.GetType().Name.ShouldBe("DivisionNoEliminableException");
	}

	[Fact]
	public void ValidarSePuedeEliminar_de_primaria_con_matricula_suficiente_lanza_DivisionConMatriculaSuficienteException()
	{
		var division = new DivisionBuilder().Build();

		var ex = Should.Throw<Exception>(() => division.ValidarSePuedeEliminar(NivelEducativo.Primaria, 15));

		ex.GetType().Name.ShouldBe("DivisionConMatriculaSuficienteException");
	}

	[Fact]
	public void ValidarSePuedeEliminar_de_primaria_con_poca_matricula_no_lanza()
	{
		var division = new DivisionBuilder().Build();

		Should.NotThrow(() => division.ValidarSePuedeEliminar(NivelEducativo.Primaria, 14));
	}
	#endregion
}
