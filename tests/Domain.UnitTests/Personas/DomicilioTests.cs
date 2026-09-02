using Domain.Personas.Domicilios;
using Domain.Shared.Exceptions;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Personas;

/// <summary>
/// Value objects <see cref="Domicilio"/>, <see cref="Direccion"/> y <see cref="Ubicacion"/>:
/// camino válido, invariantes de cada factoría e igualdad estructural.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class DomicilioTests
{
	#region Domicilio — camino válido e igualdad
	[Fact]
	public void Crear_compone_direccion_y_ubicacion_con_los_datos_dados()
	{
		var domicilio = new DomicilioBuilder()
			.ConCalle("Pasaje Ñuñorco")
			.ConAltura("1234")
			.ConVivienda(Vivienda.Departamento)
			.ConLocalidad("Yerba Buena")
			.ConProvincia("Tucumán")
			.ConPais("Argentina")
			.Build();

		domicilio.Direccion.Calle.ShouldBe("Pasaje Ñuñorco");
		domicilio.Direccion.Altura.ShouldBe("1234");
		domicilio.Direccion.Vivienda.ShouldBe(Vivienda.Departamento);
		domicilio.Ubicacion.Localidad.ShouldBe("Yerba Buena");
		domicilio.Ubicacion.Provincia.ShouldBe("Tucumán");
		domicilio.Ubicacion.Pais.ShouldBe("Argentina");
	}

	[Fact]
	public void Dos_domicilios_con_los_mismos_datos_son_iguales()
	{
		var uno = new DomicilioBuilder().Build();
		var otro = new DomicilioBuilder().Build();

		uno.Equals(otro).ShouldBeTrue();
		uno.GetHashCode().ShouldBe(otro.GetHashCode());
	}

	[Fact]
	public void Cambiar_la_localidad_rompe_la_igualdad()
	{
		var uno = new DomicilioBuilder().ConLocalidad("Yerba Buena").Build();
		var otro = new DomicilioBuilder().ConLocalidad("Tafí Viejo").Build();

		uno.Equals(otro).ShouldBeFalse();
	}
	#endregion

	#region Direccion
	[Fact]
	public void Direccion_recorta_los_espacios_de_la_calle_y_la_observacion()
	{
		var direccion = Direccion.Crear("  Pasaje Ñuñorco  ", "1234", nameof(Vivienda.Casa), "  timbre a la derecha  ");

		direccion.Calle.ShouldBe("Pasaje Ñuñorco");
		direccion.Observacion.ShouldBe("timbre a la derecha");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Direccion_sin_calle_lanza_ArgumentNullException(string calle)
	{
		Should.Throw<ArgumentNullException>(() =>
			Direccion.Crear(calle, "1234", nameof(Vivienda.Casa), string.Empty));
	}

	[Fact]
	public void Direccion_con_observacion_de_mas_de_120_caracteres_lanza_ExcesoCaracteresException()
	{
		var observacion = new string('á', 121);

		Should.Throw<ExcesoCaracteresException>(() =>
			Direccion.Crear("Pasaje Ñuñorco", "1234", nameof(Vivienda.Casa), observacion));
	}

	[Fact]
	public void Direccion_con_vivienda_fuera_del_enum_lanza_ArgumentException()
	{
		Should.Throw<ArgumentException>(() =>
			Direccion.Crear("Pasaje Ñuñorco", "1234", "Rancho", string.Empty));
	}
	#endregion

	#region Ubicacion
	[Fact]
	public void Ubicacion_recorta_los_espacios_de_cada_campo()
	{
		var ubicacion = Ubicacion.Crear("  Yerba Buena ", " Tucumán ", " Argentina ");

		ubicacion.Localidad.ShouldBe("Yerba Buena");
		ubicacion.Provincia.ShouldBe("Tucumán");
		ubicacion.Pais.ShouldBe("Argentina");
	}

	[Theory]
	[InlineData("", "Tucumán", "Argentina")]
	[InlineData("Yerba Buena", "   ", "Argentina")]
	[InlineData("Yerba Buena", "Tucumán", "")]
	public void Ubicacion_con_un_campo_vacio_lanza_ArgumentNullException(string localidad, string provincia, string pais)
	{
		Should.Throw<ArgumentNullException>(() => Ubicacion.Crear(localidad, provincia, pais));
	}

	[Fact]
	public void Dos_ubicaciones_con_los_mismos_datos_son_iguales()
	{
		var uno = Ubicacion.Crear("Yerba Buena", "Tucumán", "Argentina");
		var otro = Ubicacion.Crear("Yerba Buena", "Tucumán", "Argentina");

		uno.Equals(otro).ShouldBeTrue();
	}
	#endregion
}
