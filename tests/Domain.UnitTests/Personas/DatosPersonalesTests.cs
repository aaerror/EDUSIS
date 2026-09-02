using Domain.Personas;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Personas;

/// <summary>
/// Value object <see cref="DatosPersonales"/>: camino válido, invariantes del constructor
/// (<c>DatosPersonales.Crear</c>) e igualdad estructural vía <c>GetEqualityCommponents()</c>.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class DatosPersonalesTests
{
	#region Camino válido
	[Fact]
	public void Crear_con_datos_completos_conserva_cada_campo()
	{
		var datos = new DatosPersonalesBuilder()
			.ConApellidoYNombre("Ñáñez", "José María")
			.ConDocumento("30111222")
			.ConSexo(Sexo.Masculino)
			.ConFechaNacimiento(new DateTime(1985, 4, 20))
			.ConNacionalidad("Argentina")
			.Build();

		datos.Apellido.ShouldBe("Ñáñez");
		datos.Nombre.ShouldBe("José María");
		datos.Documento.ShouldBe("30111222");
		datos.Sexo.ShouldBe(Sexo.Masculino);
		datos.FechaNacimiento.ShouldBe(new DateTime(1985, 4, 20));
		datos.Nacionalidad.ShouldBe("Argentina");
	}

	[Fact]
	public void NombreCompleto_devuelve_apellido_coma_nombre()
	{
		var datos = new DatosPersonalesBuilder().ConApellidoYNombre("Ñáñez", "José María").Build();

		datos.NombreCompleto().ShouldBe("Ñáñez, José María");
	}

	[Fact]
	public void Edad_es_la_diferencia_de_años_calendario()
	{
		var datos = new DatosPersonalesBuilder().ConEdad(31).Build();

		datos.Edad().ShouldBe(31);
	}

	[Fact]
	public void La_fecha_de_nacimiento_se_normaliza_a_medianoche()
	{
		var conHora = new DateTime(1990, 6, 15, 13, 45, 0);

		var datos = new DatosPersonalesBuilder().ConFechaNacimiento(conHora).Build();

		datos.FechaNacimiento.ShouldBe(conHora.Date);
	}
	#endregion

	#region Invariantes
	[Theory]
	[InlineData("", "José", "30111222", "Argentina")]
	[InlineData("Ñáñez", "  ", "30111222", "Argentina")]
	[InlineData("Ñáñez", "José", "  ", "Argentina")]
	[InlineData("Ñáñez", "José", "30111222", "")]
	public void Crear_con_un_campo_de_texto_vacio_lanza_ArgumentNullException(string apellido, string nombre, string documento, string nacionalidad)
	{
		Should.Throw<ArgumentNullException>(() =>
			DatosPersonales.Crear(apellido, nombre, documento, nameof(Sexo.Femenino), new DateTime(1990, 1, 1), nacionalidad));
	}

	[Theory]
	[InlineData("123456")]
	[InlineData("123456789")]
	[InlineData("30.111.222")]
	[InlineData("ABCDEFGH")]
	public void Crear_con_documento_de_formato_invalido_lanza_FormatException(string documento)
	{
		Should.Throw<FormatException>(() =>
			DatosPersonales.Crear("Ñáñez", "José", documento, nameof(Sexo.Femenino), new DateTime(1990, 1, 1), "Argentina"));
	}

	[Fact]
	public void Crear_con_fecha_de_nacimiento_futura_lanza_ArgumentException()
	{
		var futuro = DateTime.Today.AddDays(1);

		Should.Throw<ArgumentException>(() =>
			DatosPersonales.Crear("Ñáñez", "José", "30111222", nameof(Sexo.Femenino), futuro, "Argentina"));
	}

	[Fact]
	public void Crear_con_un_sexo_que_no_es_del_enum_lanza_ArgumentException()
	{
		Should.Throw<ArgumentException>(() =>
			DatosPersonales.Crear("Ñáñez", "José", "30111222", "Otro", new DateTime(1990, 1, 1), "Argentina"));
	}
	#endregion

	#region Igualdad estructural
	[Fact]
	public void Dos_datos_personales_con_los_mismos_componentes_son_iguales()
	{
		var unos = new DatosPersonalesBuilder().Build();
		var otros = new DatosPersonalesBuilder().Build();

		unos.Equals(otros).ShouldBeTrue();
		unos.GetHashCode().ShouldBe(otros.GetHashCode());
	}

	[Fact]
	public void Cambiar_el_apellido_rompe_la_igualdad()
	{
		var unos = new DatosPersonalesBuilder().ConApellido("Ñáñez").Build();
		var otros = new DatosPersonalesBuilder().ConApellido("Gómez").Build();

		unos.Equals(otros).ShouldBeFalse();
	}

	[Fact]
	public void El_documento_no_participa_de_la_igualdad_estructural()
	{
		var unos = new DatosPersonalesBuilder().ConDocumento("30111222").Build();
		var otros = new DatosPersonalesBuilder().ConDocumento("40999888").Build();

		unos.Equals(otros).ShouldBeTrue();
	}
	#endregion
}
