using Domain.Personas;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Personas;

/// <summary>
/// Value object <see cref="DatosNacimiento"/> y <c>FechaNacimientoInvalidaException</c>
/// (<c>internal</c>: se verifica por el nombre del tipo). Es la única vía por la que esa
/// excepción del módulo <c>Personas</c> es alcanzable.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class DatosNacimientoTests
{
	#region Camino válido
	[Fact]
	public void Crear_con_una_fecha_pasada_normaliza_a_medianoche()
	{
		var fecha = new DateTime(1990, 6, 15, 8, 30, 0);

		var datos = DatosNacimiento.Crear(fecha);

		datos.FechaNacimiento.ShouldBe(fecha.Date);
	}

	[Fact]
	public void Edad_es_la_diferencia_de_años_calendario()
	{
		var datos = DatosNacimiento.Crear(DateTime.Today.AddYears(-25));

		datos.Edad.ShouldBe(25);
	}

	[Fact]
	public void EsMayorEdad_es_true_para_mas_de_18_años()
	{
		DatosNacimiento.Crear(DateTime.Today.AddYears(-19)).EsMayorEdad().ShouldBeTrue();
	}

	[Fact]
	public void EsMayorEdad_es_false_para_18_años_exactos()
	{
		DatosNacimiento.Crear(DateTime.Today.AddYears(-18)).EsMayorEdad().ShouldBeFalse();
	}

	[Fact]
	public void Dos_datos_de_nacimiento_con_la_misma_fecha_son_iguales()
	{
		var uno = DatosNacimiento.Crear(new DateTime(1990, 6, 15));
		var otro = DatosNacimiento.Crear(new DateTime(1990, 6, 15));

		uno.Equals(otro).ShouldBeTrue();
		uno.GetHashCode().ShouldBe(otro.GetHashCode());
	}
	#endregion

	#region Invariante
	[Fact]
	public void Crear_con_la_fecha_de_hoy_lanza_FechaNacimientoInvalidaException()
	{
		var ex = Should.Throw<Exception>(() => DatosNacimiento.Crear(DateTime.Today));

		ex.GetType().Name.ShouldBe("FechaNacimientoInvalidaException");
	}

	[Fact]
	public void Crear_con_una_fecha_futura_lanza_FechaNacimientoInvalidaException()
	{
		var ex = Should.Throw<Exception>(() => DatosNacimiento.Crear(DateTime.Today.AddYears(1)));

		ex.GetType().Name.ShouldBe("FechaNacimientoInvalidaException");
	}
	#endregion
}
