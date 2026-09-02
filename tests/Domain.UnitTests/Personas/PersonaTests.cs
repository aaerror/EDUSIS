using Domain.Personas;
using Domain.Personas.Domicilios;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Personas;

/// <summary>
/// Base <see cref="Persona"/> (abstracta): camino válido del constructor, mutaciones de nombre,
/// sexo, domicilio y contacto, e invariantes. <c>FormatoEmailInvalidoException</c> y
/// <c>FormatoTelefonoInvalidoException</c> son <c>internal</c>: se verifican por el nombre del
/// tipo. Se usa una <see cref="Persona"/> de prueba definida en este archivo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class PersonaTests
{
	private static PersonaDePrueba CrearPersona() =>
		new(
			new DatosPersonalesBuilder().Build(),
			new DomicilioBuilder().Build(),
			"maria.gonzalez@correo.com",
			"3814123456");

	#region Camino válido
	[Fact]
	public void El_constructor_asigna_datos_personales_domicilio_email_y_telefono()
	{
		var datos = new DatosPersonalesBuilder().ConApellidoYNombre("Ñáñez", "José María").Build();
		var domicilio = new DomicilioBuilder().Build();

		var persona = new PersonaDePrueba(datos, domicilio, "maria.gonzalez@correo.com", "3814123456");

		persona.DatosPersonales.ShouldBe(datos);
		persona.Domicilio.ShouldBe(domicilio);
		persona.Email.ShouldBe("maria.gonzalez@correo.com");
		persona.Telefono.ShouldBe("3814123456");
		persona.Id.ShouldNotBe(Guid.Empty);
	}

	[Fact]
	public void CambiarNombreCompleto_actualiza_los_datos_personales()
	{
		var persona = CrearPersona();

		persona.CambiarNombreCompleto("Gómez", "Lucía");

		persona.DatosPersonales.NombreCompleto().ShouldBe("Gómez, Lucía");
	}

	[Fact]
	public void CambiarSexo_actualiza_el_sexo_registrado()
	{
		var persona = CrearPersona();

		persona.CambiarSexo("Ñáñez", "José", nameof(Sexo.Masculino));

		persona.DatosPersonales.Sexo.ShouldBe(Sexo.Masculino);
	}

	[Fact]
	public void CambiarDomicilio_reemplaza_el_domicilio()
	{
		var persona = CrearPersona();
		var nuevo = new DomicilioBuilder().ConLocalidad("Tafí Viejo").Build();

		persona.CambiarDomicilio(nuevo);

		persona.Domicilio.ShouldBe(nuevo);
	}

	[Fact]
	public void CambiarDireccion_reemplaza_la_direccion_y_conserva_la_ubicacion()
	{
		var persona = CrearPersona();
		var ubicacionOriginal = persona.Domicilio.Ubicacion;
		var nuevaDireccion = Direccion.Crear("Avenida Perón", "999", nameof(Vivienda.Departamento), string.Empty);

		persona.CambiarDireccion(nuevaDireccion);

		persona.Domicilio.Direccion.Calle.ShouldBe("Avenida Perón");
		persona.Domicilio.Ubicacion.ShouldBe(ubicacionOriginal);
	}

	[Fact]
	public void CambiarContacto_actualiza_email_y_telefono_validos()
	{
		var persona = CrearPersona();

		persona.CambiarContacto("nuevo.correo@dominio.org", "3814999888");

		persona.Email.ShouldBe("nuevo.correo@dominio.org");
		persona.Telefono.ShouldBe("3814999888");
	}
	#endregion

	#region Invariantes
	[Fact]
	public void CambiarSexo_al_mismo_sexo_registrado_lanza_ArgumentException()
	{
		var persona = CrearPersona();

		Should.Throw<ArgumentException>(() => persona.CambiarSexo("Ñáñez", "José", nameof(Sexo.Femenino)));
	}

	[Fact]
	public void CambiarDomicilio_con_null_lanza_ArgumentNullException()
	{
		var persona = CrearPersona();

		Should.Throw<ArgumentNullException>(() => persona.CambiarDomicilio(null!));
	}

	[Fact]
	public void El_constructor_con_datos_personales_null_lanza_ArgumentNullException()
	{
		Should.Throw<ArgumentNullException>(() =>
			new PersonaDePrueba(null!, new DomicilioBuilder().Build(), "maria.gonzalez@correo.com", "3814123456"));
	}

	[Fact]
	public void El_constructor_con_domicilio_null_lanza_ArgumentNullException()
	{
		Should.Throw<ArgumentNullException>(() =>
			new PersonaDePrueba(new DatosPersonalesBuilder().Build(), null!, "maria.gonzalez@correo.com", "3814123456"));
	}

	[Theory]
	[InlineData("sin-arroba")]
	[InlineData("mal@dominio.")]
	[InlineData("mal@dominio.xyz")]
	public void El_constructor_con_email_de_formato_invalido_lanza_FormatoEmailInvalidoException(string email)
	{
		var ex = Should.Throw<Exception>(() =>
			new PersonaDePrueba(new DatosPersonalesBuilder().Build(), new DomicilioBuilder().Build(), email, "3814123456"));

		ex.GetType().Name.ShouldBe("FormatoEmailInvalidoException");
	}

	[Theory]
	[InlineData("12345")]
	[InlineData("no-es-un-numero")]
	[InlineData("00000000")]
	public void El_constructor_con_telefono_de_formato_invalido_lanza_FormatoTelefonoInvalidoException(string telefono)
	{
		var ex = Should.Throw<Exception>(() =>
			new PersonaDePrueba(new DatosPersonalesBuilder().Build(), new DomicilioBuilder().Build(), "maria.gonzalez@correo.com", telefono));

		ex.GetType().Name.ShouldBe("FormatoTelefonoInvalidoException");
	}

	[Fact]
	public void CambiarContacto_con_email_invalido_lanza_FormatoEmailInvalidoException()
	{
		var persona = CrearPersona();

		var ex = Should.Throw<Exception>(() => persona.CambiarContacto("mal", "3814123456"));

		ex.GetType().Name.ShouldBe("FormatoEmailInvalidoException");
	}
	#endregion
}

#region Doble de prueba
internal sealed class PersonaDePrueba : Persona
{
	public PersonaDePrueba(DatosPersonales datosPersonales, Domicilio domicilio, string email, string telefono)
		: base(datosPersonales, domicilio, email, telefono)
	{
	}
}
#endregion
