using Domain.Divisiones;
using Domain.Shared;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Divisiones;

/// <summary>
/// Agregado <see cref="Division"/>: alta, numeración automática (<see cref="Division.Siguiente"/>),
/// cargo de preceptor (asignar, cambiar y quitar) y las validaciones de cupo/baja. La gestión de cursantes por división pasó
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
	public void Siguiente_hasta_la_Z_sigue_proponiendo_la_letra_siguiente()
	{
		var division = Division.Siguiente(Guid.NewGuid(), new[] { "X", "Y" });

		division.Descripcion.ShouldBe("Z");
	}

	[Fact]
	public void Siguiente_despues_de_Z_lanza_SinLetrasDisponiblesException()
	{
		// Cierra el TODO "sin tope tras 'Z'" de Division.Siguiente: char.ConvertFromUtf32('Z' + 1)
		// daba "[", que el constructor rechazaba con un ArgumentException genérico ("la división
		// del curso debe ser una letra"). Ahora la regla de negocio —el curso agotó sus letras—
		// tiene excepción propia y mensaje que explica el límite.
		var ex = Should.Throw<Exception>(() => Division.Siguiente(Guid.NewGuid(), new[] { "Z" }));

		ex.GetType().Name.ShouldBe("SinLetrasDisponiblesException");
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
	public void CambiarPreceptor_reemplaza_al_preceptor_del_cargo_ocupado()
	{
		var division = new DivisionBuilder().Build();
		var anterior = Guid.NewGuid();
		var nuevo = Guid.NewGuid();
		division.AsignarPreceptor(anterior);

		division.CambiarPreceptor(nuevo);

		division.Preceptor.ShouldBe(nuevo);
		division.EstaCargoPreceptorVacante().ShouldBeFalse();
	}

	[Fact]
	public void CambiarPreceptor_sobre_un_cargo_vacante_lanza_CargoPreceptorDisponibleException()
	{
		var division = new DivisionBuilder().Build();

		var ex = Should.Throw<Exception>(() => division.CambiarPreceptor(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("CargoPreceptorDisponibleException");
	}

	[Fact]
	public void CambiarPreceptor_con_Guid_vacio_lanza_SinDatosPreceptorException()
	{
		var division = new DivisionBuilder().Build();
		division.AsignarPreceptor(Guid.NewGuid());

		var ex = Should.Throw<Exception>(() => division.CambiarPreceptor(Guid.Empty));

		ex.GetType().Name.ShouldBe("SinDatosPreceptorException");
	}

	[Fact]
	public void CambiarPreceptor_con_el_preceptor_que_ya_tiene_lanza_PreceptorSinCambiosException()
	{
		var division = new DivisionBuilder().Build();
		var preceptor = Guid.NewGuid();
		division.AsignarPreceptor(preceptor);

		var ex = Should.Throw<Exception>(() => division.CambiarPreceptor(preceptor));

		ex.GetType().Name.ShouldBe("PreceptorSinCambiosException");
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
	// Las dos reglas conviven (decisión de B2/DIV-2): una división vacía se elimina siempre, de
	// cualquier nivel, y el nivel sólo decide cuando quedan cursantes. Antes de DIV-2 estos casos
	// afirmaban que Secundaria rechazaba el borrado incluso con la división vacía.
	[Theory]
	[InlineData(NivelEducativo.Secundaria)]
	[InlineData(NivelEducativo.Primaria)]
	public void ValidarSePuedeEliminar_sin_cursantes_no_lanza_en_ningun_nivel(NivelEducativo nivel)
	{
		var division = new DivisionBuilder().Build();

		Should.NotThrow(() => division.ValidarSePuedeEliminar(nivel, 0));
	}

	[Fact]
	public void ValidarSePuedeEliminar_de_secundaria_con_cursantes_lanza_DivisionNoEliminableException()
	{
		// Rama inalcanzable desde la aplicación —ServicioDivision.EliminarDivisionAsync exige la
		// división vacía de inscripciones antes de llegar acá— y afirmada a propósito: es la regla
		// de negocio del dominio y lo único que la mantiene viva (ver el <remarks> del método).
		var division = new DivisionBuilder().Build();

		var ex = Should.Throw<Exception>(() => division.ValidarSePuedeEliminar(NivelEducativo.Secundaria, 1));

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
